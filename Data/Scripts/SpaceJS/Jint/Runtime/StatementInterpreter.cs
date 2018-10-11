using System.Collections.Generic;
using Esprima.Ast;
using Jint.Native;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Environments;
using Jint.Runtime.References;
using System;
using Sandbox.ModAPI;

namespace Jint.Runtime
{
    public sealed class StatementInterpreter
    {
        private readonly Engine _engine;

        Stack<RuntimeState> stack = new Stack<RuntimeState>();

        public StatementInterpreter(Engine engine)
        {
            _engine = engine;
        }

        private void ExecuteStatement(RuntimeState state)
        {
            Statement statement = (Statement)state.arg;
            if(state.calleeReturned)
            {
                Return(state.calleeReturnValue);
                return;
            }

            Call(_engine.ExecuteStatement, statement);
        }

        public void ExecuteEmptyStatement(RuntimeState state)
        {
            EmptyStatement emptyStatement = (EmptyStatement)state.arg;
            Return(new Completion(CompletionType.Normal, null, null));
            return;
        }

        public void ExecuteExpressionStatement(RuntimeState state)
        {
            ExpressionStatement expressionStatement = (ExpressionStatement)state.arg;
            // TODO - possibly will need to add expressions to the state machine.
            var exprRef = _engine.EvaluateExpression(expressionStatement.Expression);
            Return(new Completion(CompletionType.Normal, _engine.GetValue(exprRef, true), null));
            return;
        }

        public void ExecuteIfStatement(RuntimeState state)
        {
            IfStatement ifStatement = (IfStatement)state.arg;
            if(state.calleeReturned)
            {
                Return(state.calleeReturnValue);
                return;
            }

            // TODO - possibly will need to add expressions to the state machine.
            if (TypeConverter.ToBoolean(_engine.GetValue(_engine.EvaluateExpression(ifStatement.Test), true)))
            {
                Call(_engine.ExecuteStatement, ifStatement.Consequent);
            }
            else if (ifStatement.Alternate != null)
            {
                Call(_engine.ExecuteStatement, ifStatement.Alternate);
            }
            else
            {
                Return(new Completion(CompletionType.Normal, null, null));
            }

            return;
        }

        public void ExecuteLabeledStatement(RuntimeState state)
        {
            LabeledStatement labeledStatement = (LabeledStatement)state.arg;

            // TODO: Esprima added Statement.Label, maybe not necessary as this line is finding the
            // containing label and could keep a table per program with all the labels
            // labeledStatement.Body.LabelSet = labeledStatement.Label;
            if(state.calleeReturned)
            {
                Completion result = (Completion)state.calleeReturnValue;
                if (result.Type == CompletionType.Break && result.Identifier == labeledStatement.Label.Name)
                {
                    var value = result.Value;
                    Return(new Completion(CompletionType.Normal, value, null));
                    return;
                }
                Return(result);
                return;
            }

            Call(_engine.ExecuteStatement, labeledStatement.Body);
        }

        public class ExecuteDoWhileStatementLocal
        {
            public JsValue v;
            public bool iterating;

        };

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-12.6.1
        /// </summary>
        /// <param name="doWhileStatement"></param>
        /// <returns></returns>
        public void ExecuteDoWhileStatement(RuntimeState state)
        {
            DoWhileStatement doWhileStatement = (DoWhileStatement)state.arg;

            ExecuteDoWhileStatementLocal local;

            if(state.local == null)
            {
                state.local = local = new ExecuteDoWhileStatementLocal();
                local.v = Undefined.Instance;
                local.iterating = false;
            }
            else
            {
                local = (ExecuteDoWhileStatementLocal)state.local;
            }

            if(state.calleeReturned)
            {
                var stmt = (Completion)state.calleeReturnValue;
                if (!ReferenceEquals(stmt.Value, null))
                {
                    local.v = stmt.Value;
                }
                if (stmt.Type != CompletionType.Continue || stmt.Identifier != doWhileStatement?.LabelSet?.Name)
                {
                    if (stmt.Type == CompletionType.Break && (stmt.Identifier == null || stmt.Identifier == doWhileStatement?.LabelSet?.Name))
                    {
                        Return(new Completion(CompletionType.Normal, local.v, null));
                        return;
                    }

                    if (stmt.Type != CompletionType.Normal)
                    {
                        Return(stmt);
                        return;
                    }
                }

                var exprRef = _engine.EvaluateExpression(doWhileStatement.Test);
                local.iterating = TypeConverter.ToBoolean(_engine.GetValue(exprRef, true));

                if(!local.iterating)
                {

                    Return(new Completion(CompletionType.Normal, local.v, null));
                    return;
                }

            }

            Call(_engine.ExecuteStatement, doWhileStatement.Body);
            return;

        }

        public class ExecuteWhileStatementLocal
        {
            public JsValue v;
        };

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-12.6.2
        /// </summary>
        /// <param name="whileStatement"></param>
        /// <returns></returns>
        public void ExecuteWhileStatement(RuntimeState state)
        {
            WhileStatement whileStatement = (WhileStatement)state.arg;
            ExecuteWhileStatementLocal local;
            if(state.local == null)
            {
                state.local = local = new ExecuteWhileStatementLocal();
                local.v = Undefined.Instance;
            }
            else
            {
                local = (ExecuteWhileStatementLocal)state.local;
            }

            if(state.calleeReturned)
            {
                var stmt = (Completion)state.calleeReturnValue;

                if (!ReferenceEquals(stmt.Value, null))
                {
                    local.v = stmt.Value;
                }

                if (stmt.Type != CompletionType.Continue || stmt.Identifier != whileStatement?.LabelSet?.Name)
                {
                    if (stmt.Type == CompletionType.Break && (stmt.Identifier == null || stmt.Identifier == whileStatement?.LabelSet?.Name))
                    {
                        Return(new Completion(CompletionType.Normal, local.v, null));
                        return;
                    }

                    if (stmt.Type != CompletionType.Normal)
                    {
                        Return(stmt);
                        return;
                    }
                }
            }

            var jsValue = _engine.GetValue(_engine.EvaluateExpression(whileStatement.Test), true);
            if (!TypeConverter.ToBoolean(jsValue))
            {
                Return(new Completion(CompletionType.Normal, local.v, null));
            }

            Call( _engine.ExecuteStatement, whileStatement.Body);
            return;
        }

        public class ExecuteForStatementLocal
        {
            public Esprima.Ast.INode init;
            public JsValue v;
            public uint stage;
        };
        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-12.6.3
        /// </summary>
        /// <param name="forStatement"></param>
        /// <returns></returns>
        public void ExecuteForStatement(RuntimeState state)
        {
            ForStatement forStatement = (ForStatement)state.arg;
            ExecuteForStatementLocal local;
            if (state.local == null)
            {
                state.local = local = new ExecuteForStatementLocal();
                local.init = forStatement.Init;
                local.v = Undefined.Instance;
                local.stage = 0;
            }
            else
            {
                local = (ExecuteForStatementLocal)state.local;
            }

            if(local.stage == 0)
            {
                if(state.calleeReturned)
                {
                    state.calleeReturned = false;
                    state.calleeReturnValue = null;
                    local.stage = 1;
                    return;
                }

                if (local.init != null)
                {
                    if (local.init.Type == Nodes.VariableDeclaration)
                    {
                        Call(_engine.ExecuteStatement, (Statement)local.init);
                        return;
                    }
                    else
                    {
                        _engine.GetValue(_engine.EvaluateExpression(local.init), true);
                    }
                }
                local.stage = 1;
            }

            if(local.stage == 1)
            {
                if (state.calleeReturned)
                {
                    Completion stmt = (Completion)state.calleeReturnValue;
                    state.calleeReturned = false;
                    state.calleeReturnValue = null;

                    if (!ReferenceEquals(stmt.Value, null))
                    {
                        local.v = stmt.Value;
                    }

                    var stmtType = stmt.Type;
                    if (stmtType == CompletionType.Break && (stmt.Identifier == null || stmt.Identifier == forStatement?.LabelSet?.Name))
                    {
                        Return(new Completion(CompletionType.Normal, local.v, null));
                        return;
                    }
                    if (stmtType != CompletionType.Continue || ((stmt.Identifier != null) && stmt.Identifier != forStatement?.LabelSet?.Name))
                    {
                        if (stmtType != CompletionType.Normal)
                        {
                            Return(stmt);
                            return;
                        }
                    }
                    if (forStatement.Update != null)
                    {
                        _engine.GetValue(_engine.EvaluateExpression(forStatement.Update), true);
                    }
                }

                if (forStatement.Test != null)
                {
                    var testExprRef = _engine.EvaluateExpression(forStatement.Test);
                    if (!TypeConverter.ToBoolean(_engine.GetValue(testExprRef, true)))
                    {
                        Return(new Completion(CompletionType.Normal, local.v, null));
                        return;
                    }
                }

                Call(_engine.ExecuteStatement, forStatement.Body);
                return;
            }
        }

        public class ExecuteForInStatementLocal
        {
            public uint stage;

            public Identifier identifier;
            public Reference varRef;
            public JsValue experValue;
            public Native.Object.ObjectInstance obj;
            public JsValue v;
            public Native.Object.ObjectInstance cursor;
            public HashSet<string> processedKeys;

            public uint i;
            public Jint.Native.Array.ArrayInstance keys;
            public uint length;
        };
        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-12.6.4
        /// </summary>
        /// <param name="forInStatement"></param>
        /// <returns></returns>
        public void ExecuteForInStatement(RuntimeState state)
        {
            ForInStatement forInStatement = (ForInStatement)state.arg;
            ExecuteForInStatementLocal local;
            if(state.local == null)
            {
                state.local = local = new ExecuteForInStatementLocal();
                local.stage = 0;
                local.identifier = forInStatement.Left.Type == Nodes.VariableDeclaration
                    ? (Identifier)((VariableDeclaration)forInStatement.Left).Declarations[0].Id
                    : (Identifier)forInStatement.Left;

                local.varRef = _engine.EvaluateExpression(local.identifier) as Reference;
                local.experValue = _engine.GetValue(_engine.EvaluateExpression(forInStatement.Right), true);
                if (local.experValue.IsUndefined() || local.experValue.IsNull())
                {
                    Return(new Completion(CompletionType.Normal, null, null));
                    return;
                }

                local.obj = TypeConverter.ToObject(_engine, local.experValue);
                JsValue v = Null.Instance;

                // keys are constructed using the prototype chain
                local.cursor = local.obj;
                local.processedKeys = new HashSet<string>();
            }
            else
            {
                local = (ExecuteForInStatementLocal)state.local;
            }

            if(state.calleeReturned)
            {
                Completion stmt = (Completion)state.calleeReturnValue;

                if (!ReferenceEquals(stmt.Value, null))
                {
                    local.v = stmt.Value;
                }
                if (stmt.Type == CompletionType.Break)
                {
                    Return(new Completion(CompletionType.Normal, local.v, null));
                    return;
                }
                if (stmt.Type != CompletionType.Continue)
                {
                    if (stmt.Type != CompletionType.Normal)
                    {
                        Return(stmt);
                        return;
                    }
                }
            }

            if(local.stage == 0)
            {
                if (!ReferenceEquals(local.cursor, null))
                {
                    Return(new Completion(CompletionType.Normal, local.v, null));
                    return;
                }

                local.stage = 1;
                return;
            }
            if (local.stage == 1) // While loop
            {
                local.keys = _engine.Object.GetOwnPropertyNames(Undefined.Instance, Arguments.From(local.cursor)).AsArray();

                local.length = local.keys.GetLength();
                local.i = 0;
                local.stage = 2;
                return;
            }
            if(local.stage == 2) // For loop
            {
                if (local.i < local.length)
                {
                    local.stage = 3;
                }
                else
                {
                    local.stage = 4;
                }

            }
            if (local.stage == 3) // For contents
            {
                if(state.calleeReturned)
                {              
                    local.i++;
                    local.stage = 2;
                    return;
                }

                var p = local.keys.GetOwnProperty(TypeConverter.ToString(local.i)).Value.AsStringWithoutTypeCheck();

                if (local.processedKeys.Contains(p))
                {
                    local.i++;
                    local.stage = 2;
                    return;
                }

                local.processedKeys.Add(p);

                // collection might be modified by inner statement
                if (local.cursor.GetOwnProperty(p) == PropertyDescriptor.Undefined)
                {
                    local.i++;
                    local.stage = 2;
                    return;
                }

                var value = local.cursor.GetOwnProperty(p);
                if (!value.Enumerable)
                {
                    local.i++;
                    local.stage = 2;
                    return;
                }

                _engine.PutValue(local.varRef, p);
                local.i++;
                local.stage = 2;
                return;
            }
            if (local.stage == 4) // after for loop
            {
                local.cursor = local.cursor.Prototype;
                local.stage = 0;
                return;
            }
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-12.7
        /// </summary>
        /// <param name="continueStatement"></param>
        /// <returns></returns>
        public void ExecuteContinueStatement(RuntimeState state)
        {
            ContinueStatement continueStatement = (ContinueStatement)state.arg;

            Return(new Completion(
                CompletionType.Continue,
                null,
                continueStatement.Label?.Name));
            return;
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-12.8
        /// </summary>
        /// <param name="breakStatement"></param>
        /// <returns></returns>
        public void ExecuteBreakStatement(RuntimeState state)
        {
            BreakStatement breakStatement = (BreakStatement)state.arg;

            Return(new Completion(
                CompletionType.Break,
                null,
                breakStatement.Label?.Name));
            return;
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-12.9
        /// </summary>
        /// <param name="statement"></param>
        /// <returns></returns>
        public void ExecuteReturnStatement(RuntimeState state)
        {
            ReturnStatement statement = (ReturnStatement)state.arg;
            if (statement.Argument == null)
            {
                Return(new Completion(CompletionType.Return, Undefined.Instance, null));
                return;
            }

            var jsValue = _engine.GetValue(_engine.EvaluateExpression(statement.Argument), true);
            Return(new Completion(CompletionType.Return, jsValue, null));
            return;
        }

        public class ExecuteWithStatementLocal
        {
            public LexicalEnvironment oldEnv;
        };
        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-12.10
        /// </summary>
        /// <param name="withStatement"></param>
        /// <returns></returns>
        public void ExecuteWithStatement(RuntimeState state)
        {
            WithStatement withStatement = (WithStatement)state.arg;
            ExecuteWithStatementLocal local;
            if (state.local == null)
            {
                state.local = local = new ExecuteWithStatementLocal();
                var jsValue = _engine.GetValue(_engine.EvaluateExpression(withStatement.Object), true);
                var obj = TypeConverter.ToObject(_engine, jsValue);
                local.oldEnv = _engine.ExecutionContext.LexicalEnvironment;
                var newEnv = LexicalEnvironment.NewObjectEnvironment(_engine, obj, local.oldEnv, true);
                _engine.UpdateLexicalEnvironment(newEnv);
            }
            else
            {
                local = (ExecuteWithStatementLocal)state.local;
            }

            if(state.calleeReturned)
            {
                _engine.UpdateLexicalEnvironment(local.oldEnv);
                Return(state.calleeReturnValue);
                return;
            }
        }

        public class ExecuteSwitchBlockArgs
        {
            public List<SwitchCase> switchBlock;
            public JsValue input;
        };
        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-12.11
        /// </summary>
        /// <param name="switchStatement"></param>
        /// <returns></returns>
        public void ExecuteSwitchStatement(RuntimeState state)
        {
            SwitchStatement switchStatement = (SwitchStatement)state.arg;
            if(state.calleeReturned)
            {
                Completion r = (Completion)state.calleeReturnValue;

                if (r.Type == CompletionType.Break && r.Identifier == switchStatement.LabelSet?.Name)
                {
                    Return(new Completion(CompletionType.Normal, r.Value, null));
                    return;
                }
                Return(r);
                return;
            }

            var jsValue = _engine.GetValue(_engine.EvaluateExpression(switchStatement.Discriminant), true);
            ExecuteSwitchBlockArgs args = new ExecuteSwitchBlockArgs();
            args.switchBlock = switchStatement.Cases;
            args.input = jsValue;
            Call(ExecuteSwitchBlock, args);
            return;
        }

        public class ExecuteSwitchBlockLocal
        {
            public SwitchCase defaultCase;
            public JsValue v;
            public bool hit;
            public uint stage;
            public int switchBlockCount;
            public int i;
        };
        public void ExecuteSwitchBlock(RuntimeState state)
        {
            ExecuteSwitchBlockArgs args = (ExecuteSwitchBlockArgs)state.arg;
            List<SwitchCase> switchBlock = args.switchBlock;
            JsValue input = args.input;

            ExecuteSwitchBlockLocal local;
            if (state.local == null)
            {
                state.local = local = new ExecuteSwitchBlockLocal();
                local.v = Undefined.Instance;
                local.defaultCase = null;
                local.hit = false;
                local.switchBlockCount = switchBlock.Count;
                local.i = 0;
                local.stage = 0;
            }
            else
            {
                local = (ExecuteSwitchBlockLocal)state.local;
            }


            if(local.stage == 0) // for loop
            {
                if(local.i < local.switchBlockCount)
                {
                    local.stage = 1;
                }
                else
                {
                    local.stage = 2;
                }
            }
            if(local.stage == 1) // inside for loop
            {
                if(state.calleeReturned)
                {
                    Completion r = (Completion)state.calleeReturnValue;
                    if (r.Type != CompletionType.Normal)
                    {
                        Return(r);
                        return;
                    }

                    local.v = r.Value ?? Undefined.Instance;
                }

                var clause = switchBlock[local.i];
                if (clause.Test == null)
                {
                    local.defaultCase = clause;
                }
                else
                {
                    var clauseSelector = _engine.GetValue(_engine.EvaluateExpression(clause.Test), true);
                    if (ExpressionInterpreter.StrictlyEqual(clauseSelector, input))
                    {
                        local.hit = true;
                    }
                }

                if (local.hit && clause.Consequent != null)
                {
                    Call(ExecuteStatementList, clause.Consequent);
                    return;
                }

                local.i++;
                local.stage = 0;
                return;
            }

            if (local.stage == 2)
            {
                if (state.calleeReturned)
                {
                    Completion r = (Completion)state.calleeReturnValue;
                    if (r.Type != CompletionType.Normal)
                    {
                        Return(r);
                        return;
                    }

                    local.v = r.Value ?? Undefined.Instance;
                }

                // do we need to execute the default case ?
                if (local.hit == false && local.defaultCase != null)
                {
                    Call(ExecuteStatementList, local.defaultCase.Consequent);
                    return;
                }

                Return(new Completion(CompletionType.Normal, local.v, null));
                return;
            }
        }

        public class ExecuteMultipleStatementsLocal
        {
            public Completion sl;
            public Completion c;
            public int statementListCount;

            public int i;
            public uint stage;
        };

        private void ExecuteMultipleStatements(RuntimeState state)
        {
            List<StatementListItem> statementList = (List < StatementListItem >)state.arg;
            ExecuteMultipleStatementsLocal local;
            if (state.local == null)
            {
                state.local = local = new ExecuteMultipleStatementsLocal();
                local.c = new Completion(CompletionType.Normal, null, null);
                local.sl = local.c;
                local.statementListCount = statementList.Count;
                local.stage = 0;
                local.i = 0;
            }
            else
            {
                local = (ExecuteMultipleStatementsLocal)state.local;
            }

            if(local.stage == 0) // for loop
            {
                if (local.i < local.statementListCount)
                {
                    local.stage = 1;
                }
                else
                {
                    local.stage = 2;
                }
            }

            if(local.stage == 1) // inside for loop
            {
                if (state.calleeReturned)
                {
                    local.c = (Completion)state.calleeReturnValue;

                    if (local.c.Type != CompletionType.Normal)
                    {
                        var executeStatementList = new Completion(
                            local.c.Type,
                            local.c.Value ?? local.sl.Value,
                            local.c.Identifier,
                            local.c.Location);

                        Return(executeStatementList);
                        return;
                    }

                    local.sl = local.c;
                    state.calleeReturned = false;
                    local.i++;
                    local.stage = 0;
                    return;
                }
                Call(_engine.ExecuteStatement,(Statement)statementList[local.i]);
                return;
            }

            if(local.stage == 2) // after for loop
            {
                Return(new Completion(local.c.Type, local.c.GetValueOrDefault(), local.c.Identifier));
                return;
            }
        }

        private void ExecuteSingleStatement(RuntimeState state)
        {
            if (state.calleeReturned)
            {
                var c = (Completion)state.calleeReturnValue;
                if (c.Type != CompletionType.Normal)
                {
                    var completion = new Completion(
                        c.Type,
                        c.Value,
                        c.Identifier,
                        c.Location);
                    Return(completion);
                }
                else
                {
                    Return(new Completion(c.Type, c.GetValueOrDefault(), c.Identifier));
                }
                return;
            }
            Statement s = (Statement)state.arg;
            this.Call(_engine.ExecuteStatement, s);
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-12.13
        /// </summary>
        /// <param name="throwStatement"></param>
        /// <returns></returns>
        public void ExecuteThrowStatement(RuntimeState state)
        {
            ThrowStatement throwStatement = (ThrowStatement)state.arg;
            var jsValue = _engine.GetValue(_engine.EvaluateExpression(throwStatement.Argument), true);
            Return(new Completion(CompletionType.Throw, jsValue, null, throwStatement.Location));
            return;
        }

        public class ExecuteTryStatementLocal
        {
            public uint stage;
            public LexicalEnvironment oldEnv;
            public Completion b;
        };

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-12.14
        /// </summary>
        /// <param name="tryStatement"></param>
        /// <returns></returns>
        public void ExecuteTryStatement(RuntimeState state)
        {
            TryStatement tryStatement = (TryStatement)state.arg;
            ExecuteTryStatementLocal local;
            if (state.local == null)
            {
                state.local = local = new ExecuteTryStatementLocal();
                local.stage = 0;
            }
            else
            {
                local = (ExecuteTryStatementLocal)state.local;
            }

            if(local.stage == 0)
            {
                if(state.calleeReturned)
                {
                    state.calleeReturned = false;
                    local.stage = 1;
                    local.b = (Completion)state.calleeReturnValue;
                    return;
                }

                Call(_engine.ExecuteStatement, tryStatement.Block);
                return;
            }

            if (local.stage == 1)
            {
                if (state.calleeReturned)
                {
                    state.calleeReturned = false;
                    local.stage = 2;
                    local.b = (Completion)state.calleeReturnValue;
                    return;
                }

                var catchClause = tryStatement.Handler;
                if (catchClause != null)
                {
                    var c = local.b.Value;
                    local.oldEnv = _engine.ExecutionContext.LexicalEnvironment;
                    var catchEnv = LexicalEnvironment.NewDeclarativeEnvironment(_engine, local.oldEnv);
                    catchEnv._record.CreateMutableBinding(((Identifier)catchClause.Param).Name, c);

                    _engine.UpdateLexicalEnvironment(catchEnv);
                    Call(_engine.ExecuteStatement, catchClause.Body);
                    return;
                }
                local.stage = 2;
            }

            if (local.stage == 2)
            {
                if (state.calleeReturned)
                {
                    var f = (Completion)state.calleeReturnValue;
                    if (f.Type == CompletionType.Normal)
                    {
                        Return(local.b);
                        return;
                    }

                    Return(f);
                    return;
                }

                if (tryStatement.Finalizer != null)
                {
                    Call(_engine.ExecuteStatement,tryStatement.Finalizer);
                    return;
                }

                Return(local.b);
                return;
            }
        }

        public void ExecuteProgram(Program program)
        {
            this.Call(ExecuteStatementList, program.Body);
        }

        public void ExecuteProgram(RuntimeState state)
        {
            Program program = (Program)state.arg;
            if(state.calleeReturned)
            {
                Return(state.calleeReturnValue);
                return;
            }

            this.Call(ExecuteStatementList, program.Body);
        }

        public void ExecuteStatementList(RuntimeState state)
        {
            if(state.calleeReturned)
            {
                Return(state.calleeReturnValue);
                return;
            }
            var statementList = (List<StatementListItem>) state.arg;

            // optimize common case without loop
            if (statementList.Count == 1)
            {
                this.Call(ExecuteSingleStatement, (Statement)statementList[0]);
            }
            else
            {
                this.Call(ExecuteMultipleStatements, statementList);
            }
        }

        public void Return(object o)
        {
            this.stack.Pop();

            if(this.stack.Count > 0)
            {
                this.stack.Peek().calleeReturnValue = o;
                this.stack.Peek().calleeReturned = true;
            }
        }

        public void Call(Action<RuntimeState> method, object arg)
        {
            this.stack.Push(new RuntimeState(method, arg));
        }

        public bool Step()
        {
            if(stack.Count == 0)
            {
                return false;
            }

            RuntimeState current = stack.Peek();
            try
            {
                current.Call();
            }
            catch (JavaScriptException v)
            {
                var s = (Statement)current.arg;

                Return(new Completion(CompletionType.Throw, v.Error, null, v.Location ?? s?.Location));
            }

            return true;
        }

        public void Clear()
        {
            stack.Clear();
        }

        public void ExecuteVariableDeclaration(RuntimeState state)
        {
            VariableDeclaration statement = (VariableDeclaration) state.arg;
            var declarationsCount = statement.Declarations.Count;
            for (var i = 0; i < declarationsCount; i++)
            {
                var declaration = statement.Declarations[i];
                if (declaration.Init != null)
                {
                    var lhs = (Reference) _engine.EvaluateExpression(declaration.Id);
                    lhs.AssertValid(_engine);

                    var value = _engine.GetValue(_engine.EvaluateExpression(declaration.Init), true);
                    _engine.PutValue(lhs, value);
                    _engine._referencePool.Return(lhs);
                }
            }

            Return(new Completion(CompletionType.Normal, Undefined.Instance, null));
            return;
        }

        public void ExecuteBlockStatement(RuntimeState state)
        {
            BlockStatement blockStatement = (BlockStatement)state.arg;
            if(state.calleeReturned)
            {
                Return(state.calleeReturnValue);
                return;
            }

            Call(ExecuteStatementList, blockStatement.Body);
            return;
        }

    }
}
