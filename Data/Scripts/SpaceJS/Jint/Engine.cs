using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Esprima;
using Esprima.Ast;
using Jint.Native;
using Jint.Native.Array;
using Jint.Native.Boolean;
using Jint.Native.Error;
using Jint.Native.Function;
using Jint.Native.Global;
using Jint.Native.Iterator;
using Jint.Native.Json;
using Jint.Native.Map;
using Jint.Native.Math;
using Jint.Native.Number;
using Jint.Native.Object;
using Jint.Native.RegExp;
using Jint.Native.Set;
using Jint.Native.String;
using Jint.Native.Symbol;
using Jint.Pooling;
using Jint.Runtime;
using Jint.Runtime.CallStack;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Environments;
using Jint.Runtime.Interop;
using Jint.Runtime.References;
using Sandbox.ModAPI;

namespace Jint
{
    public class Engine
    {
        private static readonly ParserOptions DefaultParserOptions = new ParserOptions
        {
            AdaptRegexp = true,
            Tolerant = false,
            Loc = true
        };

        private readonly ExpressionInterpreter _expressions;
        private readonly StatementInterpreter _statements;
        private readonly ExecutionContextStack _executionContexts;
        private JsValue _completionValue = JsValue.Undefined;
        private int _statementsCount;
        private long _initialMemoryUsage;
        private long _timeoutTicks;
        private INode _lastSyntaxNode;

        // cached access
        private readonly bool _isDebugMode;
        private readonly bool _isStrict;
        private readonly int _maxStatements;
        private readonly long _memoryLimit;
        private readonly bool _runBeforeStatementChecks;
        private readonly IReferenceResolver _referenceResolver;
        internal readonly ReferencePool _referencePool;
        internal readonly ArgumentsInstancePool _argumentsInstancePool;
        internal readonly JsValueArrayPool _jsValueArrayPool;

//        public ITypeConverter ClrTypeConverter;

        // cache of types used when resolving CLR type names
        internal Dictionary<string, Type> TypeCache = new Dictionary<string, Type>();

        internal static Dictionary<Type, Func<Engine, object, JsValue>> TypeMappers = new Dictionary<Type, Func<Engine, object, JsValue>>
        {
            { typeof(bool), (Engine engine, object v) => (bool) v ? JsBoolean.True : JsBoolean.False },
            { typeof(byte), (Engine engine, object v) => JsNumber.Create((byte)v) },
            { typeof(char), (Engine engine, object v) => JsString.Create((char)v) },
            //{ typeof(DateTime), (Engine engine, object v) => engine.Date.Construct((DateTime)v) },
//            { typeof(DateTimeOffset), (Engine engine, object v) => engine.Date.Construct((DateTimeOffset)v) },
            { typeof(decimal), (Engine engine, object v) => (JsValue) (double)(decimal)v },
            { typeof(double), (Engine engine, object v) => (JsValue)(double)v },
            { typeof(Int16), (Engine engine, object v) => JsNumber.Create((Int16)v) },
            { typeof(Int32), (Engine engine, object v) => JsNumber.Create((Int32)v) },
            { typeof(Int64), (Engine engine, object v) => (JsValue)(Int64)v },
            { typeof(SByte), (Engine engine, object v) => JsNumber.Create((SByte)v) },
            { typeof(Single), (Engine engine, object v) => (JsValue)(Single)v },
            { typeof(string), (Engine engine, object v) => (JsValue) (string)v },
            { typeof(UInt16), (Engine engine, object v) => JsNumber.Create((UInt16)v) },
            { typeof(UInt32), (Engine engine, object v) => JsNumber.Create((UInt32)v) },
            { typeof(UInt64), (Engine engine, object v) => JsNumber.Create((UInt64)v) },
            { typeof(System.Text.RegularExpressions.Regex), (Engine engine, object v) => engine.RegExp.Construct((System.Text.RegularExpressions.Regex)v, "") }
        };

        internal struct ClrPropertyDescriptorFactoriesKey : IEquatable<ClrPropertyDescriptorFactoriesKey>
        {
            public ClrPropertyDescriptorFactoriesKey(Type type, string propertyName)
            {
                Type = type;
                PropertyName = propertyName;
            }

            internal readonly Type Type;
            internal readonly string PropertyName;

            public bool Equals(ClrPropertyDescriptorFactoriesKey other)
            {
                return Type == other.Type && PropertyName == other.PropertyName;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }
                if(!(obj is ClrPropertyDescriptorFactoriesKey)) return false;
                ClrPropertyDescriptorFactoriesKey other = (ClrPropertyDescriptorFactoriesKey)obj;
                return Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Type.GetHashCode() * 397) ^ PropertyName.GetHashCode();
                }
            }
        }

        internal readonly Dictionary<ClrPropertyDescriptorFactoriesKey, Func<Engine, object, PropertyDescriptor>> ClrPropertyDescriptorFactories =
            new Dictionary<ClrPropertyDescriptorFactoriesKey, Func<Engine, object, PropertyDescriptor>>();

        internal JintCallStack CallStack = new JintCallStack();

        static Engine()
        {
            // TODO
//            var methodInfo = null; //typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread");

  //          if (methodInfo != null)
    //        {
             //   GetAllocatedBytesForCurrentThread =  (Func<long>)Delegate.CreateDelegate(typeof(Func<long>), null, methodInfo);
      //      }
        }

        public Engine() : this(null)
        {
        }

        public Engine(Action<Options> options)
        {
            _executionContexts = new ExecutionContextStack();

            Global = GlobalObject.CreateGlobalObject(this);

            Object = ObjectConstructor.CreateObjectConstructor(this);
            Function = FunctionConstructor.CreateFunctionConstructor(this);

            Symbol = SymbolConstructor.CreateSymbolConstructor(this);
            Array = ArrayConstructor.CreateArrayConstructor(this);
            Map = MapConstructor.CreateMapConstructor(this);
            Set = SetConstructor.CreateSetConstructor(this);
            Iterator= IteratorConstructor.CreateIteratorConstructor(this);
            String = StringConstructor.CreateStringConstructor(this);
            RegExp = RegExpConstructor.CreateRegExpConstructor(this);
            Number = NumberConstructor.CreateNumberConstructor(this);
            Boolean = BooleanConstructor.CreateBooleanConstructor(this);
//            Date = DateConstructor.CreateDateConstructor(this);
            Math = MathInstance.CreateMathObject(this);
            Json = JsonInstance.CreateJsonObject(this);

            Error = ErrorConstructor.CreateErrorConstructor(this, "Error");
            EvalError = ErrorConstructor.CreateErrorConstructor(this, "EvalError");
            RangeError = ErrorConstructor.CreateErrorConstructor(this, "RangeError");
            ReferenceError = ErrorConstructor.CreateErrorConstructor(this, "ReferenceError");
            SyntaxError = ErrorConstructor.CreateErrorConstructor(this, "SyntaxError");
            TypeError = ErrorConstructor.CreateErrorConstructor(this, "TypeError");
            UriError = ErrorConstructor.CreateErrorConstructor(this, "URIError");

            GlobalSymbolRegistry = new GlobalSymbolRegistry();

            // Because the properties might need some of the built-in object
            // their configuration is delayed to a later step

            Global.Configure();

            Object.Configure();
            Object.PrototypeObject.Configure();

            Symbol.Configure();
            Symbol.PrototypeObject.Configure();

            Function.Configure();
            Function.PrototypeObject.Configure();

            Array.Configure();
            Array.PrototypeObject.Configure();

            Map.Configure();
            Map.PrototypeObject.Configure();

            Set.Configure();
            Set.PrototypeObject.Configure();

            Iterator.Configure();
            Iterator.PrototypeObject.Configure();

            String.Configure();
            String.PrototypeObject.Configure();

            RegExp.Configure();
            RegExp.PrototypeObject.Configure();

            Number.Configure();
            Number.PrototypeObject.Configure();

            Boolean.Configure();
            Boolean.PrototypeObject.Configure();

//            Date.Configure();
//            Date.PrototypeObject.Configure();

            Math.Configure();
            Json.Configure();

            Error.Configure();
            Error.PrototypeObject.Configure();

            // create the global environment http://www.ecma-international.org/ecma-262/5.1/#sec-10.2.3
            GlobalEnvironment = LexicalEnvironment.NewObjectEnvironment(this, Global, null, false);

            // create the global execution context http://www.ecma-international.org/ecma-262/5.1/#sec-10.4.1.1
            EnterExecutionContext(GlobalEnvironment, GlobalEnvironment, Global);

            Options = new Options();

            options?.Invoke(Options);

            // gather some options as fields for faster checks
            _isDebugMode = Options.IsDebugMode;
            
            if(_isDebugMode) MyAPIGateway.Utilities.ShowMessage("SpaceJS", "Debug mode enabled.");

            _isStrict = Options.IsStrict;
            _maxStatements = Options._MaxStatements;
            _referenceResolver = Options.ReferenceResolver;
            _memoryLimit = Options._MemoryLimit;
            _runBeforeStatementChecks = (_maxStatements > 0 &&_maxStatements < int.MaxValue)
                                        || Options._TimeoutInterval.Ticks > 0
                                        || _memoryLimit > 0
                                        || _isDebugMode;

            _referencePool = new ReferencePool();
            _argumentsInstancePool = new ArgumentsInstancePool(this);
            _jsValueArrayPool = new JsValueArrayPool();

            Eval = new EvalFunctionInstance(this, System.ArrayExt.Empty<string>(), LexicalEnvironment.NewDeclarativeEnvironment(this, ExecutionContext.LexicalEnvironment), StrictModeScope.IsStrictModeCode);
            Global.FastAddProperty("eval", Eval, true, false, true);

            _statements = new StatementInterpreter(this);
            _expressions = new ExpressionInterpreter(this);

/*            if (Options._IsClrAllowed)
            {
                Global.FastAddProperty("System", new NamespaceReference(this, "System"), false, false, false);
                Global.FastAddProperty("importNamespace", new ClrFunctionInstance(
                    this,
                    "importNamespace",
                    (thisObj, arguments) => new NamespaceReference(this, TypeConverter.ToString(arguments.At(0)))), false, false, false);
            }
*/
//            ClrTypeConverter = new DefaultTypeConverter(this);
        }

        public LexicalEnvironment GlobalEnvironment { get; }
        public GlobalObject Global { get; }
        public ObjectConstructor Object { get; }
        public FunctionConstructor Function { get; }
        public ArrayConstructor Array { get; }
        public MapConstructor Map { get; }
        public SetConstructor Set { get; }
        public IteratorConstructor Iterator { get; }
        public StringConstructor String { get; }
        public RegExpConstructor RegExp { get; }
        public BooleanConstructor Boolean { get; }
        public NumberConstructor Number { get; }
        //public DateConstructor Date { get; }
        public MathInstance Math { get; }
        public JsonInstance Json { get; }
        public SymbolConstructor Symbol { get; }
        public EvalFunctionInstance Eval { get; }

        public ErrorConstructor Error { get; }
        public ErrorConstructor EvalError { get; }
        public ErrorConstructor SyntaxError { get; }
        public ErrorConstructor TypeError { get; }
        public ErrorConstructor RangeError { get; }
        public ErrorConstructor ReferenceError { get; }
        public ErrorConstructor UriError { get; }

        public /*ref*/ ExecutionContext ExecutionContext
        {
            get { return /*ref*/ _executionContexts.Peek(); }
        }

        public GlobalSymbolRegistry GlobalSymbolRegistry { get; }

        internal Options Options { get; }

        private static readonly Func<long> GetAllocatedBytesForCurrentThread;

        public void EnterExecutionContext(
            LexicalEnvironment lexicalEnvironment,
            LexicalEnvironment variableEnvironment,
            JsValue thisBinding)
        {
            var context = new ExecutionContext(
                lexicalEnvironment,
                variableEnvironment,
                thisBinding);

            _executionContexts.Push(context);
        }
/*
        public Engine SetValue(string name, Delegate value)
        {
            Global.FastAddProperty(name, new DelegateWrapper(this, value), true, false, true);
            return this;
        }
*/
        public Engine SetValue(string name, string value)
        {
            return SetValue(name, (JsValue) value);
        }

        public Engine SetValue(string name, double value)
        {
            return SetValue(name, JsNumber.Create(value));
        }

        public Engine SetValue(string name, int value)
        {
            return SetValue(name, JsNumber.Create(value));
        }

        public Engine SetValue(string name, bool value)
        {
            return SetValue(name, value ? JsBoolean.True : JsBoolean.False);
        }

        public Engine SetValue(string name, JsValue value)
        {
            Global.Put(name, value, false);
            return this;
        }

        public Engine SetValue(string name, object obj)
        {
            return SetValue(name, JsValue.FromObject(this, obj));
        }

        public void LeaveExecutionContext()
        {
            _executionContexts.Pop();
        }

        /// <summary>
        /// Initializes the statements count
        /// </summary>
        public void ResetStatementsCount()
        {
            _statementsCount = 0;
        }

        public void ResetMemoryUsage()
        {
            if (GetAllocatedBytesForCurrentThread != null)
            {
                _initialMemoryUsage = GetAllocatedBytesForCurrentThread();
            }
        }

        public void ResetTimeoutTicks()
        {
            var timeoutIntervalTicks = Options._TimeoutInterval.Ticks;
            _timeoutTicks = timeoutIntervalTicks > 0 ? DateTime.UtcNow.Ticks + timeoutIntervalTicks : 0;
        }

        /// <summary>
        /// Initializes list of references of called functions
        /// </summary>
        public void ResetCallStack()
        {
            CallStack.Clear();
        }

        public Engine Execute(string source)
        {
            return Execute(source, DefaultParserOptions);
        }

        public Engine Execute(string source, ParserOptions parserOptions)
        {
            var parser = new JavaScriptParser(source, parserOptions);
            return Execute(parser.ParseProgram());
        }

        public Engine Execute(Program program)
        {
            ResetStatementsCount();

            if (_memoryLimit > 0)
            {
                ResetMemoryUsage();
            }

            ResetTimeoutTicks();
            ResetLastStatement();
            ResetCallStack();

            using (new StrictModeScope(_isStrict || program.Strict))
            {
                DeclarationBindingInstantiation(DeclarationBindingType.GlobalCode, program.HoistingScope.FunctionDeclarations, program.HoistingScope.VariableDeclarations, null, null);

                _statements.ExecuteProgram(program);
            }

            return this;
        }

        private void ResetLastStatement()
        {
            _lastSyntaxNode = null;
        }

        /// <summary>
        /// Gets the last evaluated statement completion value
        /// </summary>
        public JsValue GetCompletionValue()
        {
            return _completionValue;
        }

        public bool Step()
        {
            return _statements.Step();
        }

        public void Clear()
        {
            _statements.Clear();
        }

        public void ExecuteStatement(RuntimeState state)
        {
            Statement statement = (Statement)state.arg;

            //_lastSyntaxNode = statement;

            /*            if (_runBeforeStatementChecks)
                        {
                            BeforeExecuteStatement(statement);
                        }
                        */


            if (state.calleeReturned)
            {
                switch (statement.Type)
                {
                    case Nodes.BlockStatement:
                    case Nodes.VariableDeclaration:
                    case Nodes.BreakStatement:
                    case Nodes.ContinueStatement:
                    case Nodes.DoWhileStatement:
                    case Nodes.ForStatement:
                    case Nodes.ForInStatement:
                    case Nodes.IfStatement:
                    case Nodes.LabeledStatement:
                    case Nodes.SwitchStatement:
                    case Nodes.ThrowStatement:
                    case Nodes.TryStatement:
                    case Nodes.WhileStatement:
                    case Nodes.WithStatement:
                    case Nodes.Program:
                        _statements.Return(state.calleeReturnValue);
                        return;
                    case Nodes.ReturnStatement:
                        var jsValue = ((ReturnStatement)statement).Argument == null
                            ? Undefined.Instance
                            : GetValue(state.calleeReturnValue, true);

                        _statements.Return(new Completion(CompletionType.Return, jsValue, null));
                        return;

                    case Nodes.ExpressionStatement:
                        _statements.Return(new Completion(
                            CompletionType.Normal,
                            GetValue(state.calleeReturnValue, true),
                            null));
                        return;

                    default:
                        ExceptionHelper.ThrowArgumentOutOfRangeException();
                        _statements.Return(new Completion(CompletionType.Normal, null, null));
                        return;
                }
            }

            switch (statement.Type)
            {
                case Nodes.BlockStatement:
                    _statements.Call(_statements.ExecuteStatementList, ((BlockStatement)statement).Body);
                    return;

                case Nodes.ReturnStatement:
                    _statements.Call(EvaluateExpression, ((ReturnStatement)statement).Argument);
                    return;

                case Nodes.VariableDeclaration:
                    _statements.Call(_statements.ExecuteVariableDeclaration, (VariableDeclaration)statement);
                    return;

                case Nodes.BreakStatement:
                    _statements.Call(_statements.ExecuteBreakStatement, (BreakStatement)statement);
                    return;

                case Nodes.ContinueStatement:
                    _statements.Call(_statements.ExecuteContinueStatement, (ContinueStatement)statement);
                    return;

                case Nodes.DoWhileStatement:
                    _statements.Call(_statements.ExecuteDoWhileStatement, (DoWhileStatement)statement);
                    return;

                case Nodes.EmptyStatement:
                    _statements.Return(new Completion(CompletionType.Normal, null, null));
                    return;

                case Nodes.ExpressionStatement:
                    _statements.Call(EvaluateExpression, ((ExpressionStatement)statement).Expression);
                    return;

                case Nodes.ForStatement:
                    _statements.Call(_statements.ExecuteForStatement, (ForStatement)statement);
                    return;

                case Nodes.ForInStatement:
                    _statements.Call(_statements.ExecuteForInStatement, (ForInStatement)statement);
                    return;

                case Nodes.IfStatement:
                    _statements.Call(_statements.ExecuteIfStatement, (IfStatement)statement);
                    return;

                case Nodes.LabeledStatement:
                    _statements.Call(_statements.ExecuteLabeledStatement, (LabeledStatement)statement);
                    return;

                case Nodes.SwitchStatement:
                    _statements.Call(_statements.ExecuteSwitchStatement, (SwitchStatement)statement);
                    return;

                case Nodes.FunctionDeclaration:
                    _statements.Return(new Completion(CompletionType.Normal, null, null));
                    return;

                case Nodes.ThrowStatement:
                    _statements.Call(_statements.ExecuteThrowStatement, (ThrowStatement)statement);
                    return;

                case Nodes.TryStatement:
                    _statements.Call(_statements.ExecuteTryStatement, (TryStatement)statement);
                    return;

                case Nodes.WhileStatement:
                    _statements.Call(_statements.ExecuteWhileStatement, (WhileStatement)statement);
                    return;

                case Nodes.WithStatement:
                    _statements.Call(_statements.ExecuteWithStatement, (WithStatement)statement);
                    return;

                case Nodes.Program:
                    _statements.Call(_statements.ExecuteProgram, (Program)statement);
                    return;

                default:
                    ExceptionHelper.ThrowArgumentOutOfRangeException();
                    _statements.Return(new Completion(CompletionType.Normal, null, null));
                    return;
            }
        }

        private void BeforeExecuteStatement(Statement statement)
        {
            if (_maxStatements > 0 && _statementsCount++ > _maxStatements)
            {
                ExceptionHelper.ThrowStatementsCountOverflowException();
            }

            if (_timeoutTicks > 0 && _timeoutTicks < DateTime.UtcNow.Ticks)
            {
                ExceptionHelper.ThrowTimeoutException();
            }

            if (_memoryLimit > 0)
            {
                if (GetAllocatedBytesForCurrentThread != null)
                {
                    var memoryUsage = GetAllocatedBytesForCurrentThread() - _initialMemoryUsage;
                    if (memoryUsage > _memoryLimit)
                    {
                        ExceptionHelper.ThrowMemoryLimitExceededException($"Script has allocated {memoryUsage} but is limited to {_memoryLimit}");
                    }
                }
                else
                {
                    ExceptionHelper.ThrowPlatformNotSupportedException("The current platform doesn't support MemoryLimit.");
                }
            }
        }

        public void EvaluateExpression(RuntimeState state)
        {
            INode expression = (INode)state.arg;

            if (state.calleeReturned)
            {
                Return(state.calleeReturnValue);
                return;
            }
            _lastSyntaxNode = expression;

            switch (expression.Type)
            {
                case Nodes.AssignmentExpression:
                    Call(_expressions.EvaluateAssignmentExpression, (AssignmentExpression)expression);
                    return;

                case Nodes.ArrayExpression:
                    Call(_expressions.EvaluateArrayExpression, (ArrayExpression)expression);
                    return;

                case Nodes.BinaryExpression:
                    Call(_expressions.EvaluateBinaryExpression, (BinaryExpression)expression);
                    return;

                case Nodes.CallExpression:
                    Call(_expressions.EvaluateCallExpression, (CallExpression)expression);
                    return;

                case Nodes.ConditionalExpression:
                    Call(_expressions.EvaluateConditionalExpression, (ConditionalExpression)expression);
                    return;

                case Nodes.FunctionExpression:
                    Return(_expressions.EvaluateFunctionExpression((IFunction)expression));
                    return;

                case Nodes.Identifier:
                    Return(_expressions.EvaluateIdentifier((Identifier)expression));
                    return;

                case Nodes.Literal:
                    Return(_expressions.EvaluateLiteral((Literal)expression));
                    return;

                case Nodes.LogicalExpression:
                    Call(_expressions.EvaluateLogicalExpression, (BinaryExpression)expression);
                    return;

                case Nodes.MemberExpression:
                    Call(_expressions.EvaluateMemberExpression, (MemberExpression)expression);
                    return;

                case Nodes.NewExpression:
                    Call(_expressions.EvaluateNewExpression, (NewExpression)expression);
                    return;

                case Nodes.ObjectExpression:
                    Call(_expressions.EvaluateObjectExpression, (ObjectExpression)expression);
                    return;

                case Nodes.SequenceExpression:
                    Call(_expressions.EvaluateSequenceExpression, (SequenceExpression)expression);
                    return;

                case Nodes.ThisExpression:
                    Return(_expressions.EvaluateThisExpression((ThisExpression)expression));
                    return;

                case Nodes.UpdateExpression:
                    Call(_expressions.EvaluateUpdateExpression, (UpdateExpression)expression);
                    return;

                case Nodes.UnaryExpression:
                    Call(_expressions.EvaluateUnaryExpression, (UnaryExpression)expression);
                    return;

                default:
                    ExceptionHelper.ThrowArgumentOutOfRangeException();
                    Return(null);
                    return;
            }
        }


        public void Call(Action<RuntimeState> method, object arg)
        {
            _statements.Call(method, arg);
        }

        public void Return(object o)
        {
            _statements.Return(o);
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-8.7.1
        /// </summary>
        public JsValue GetValue(object value)
        {
            return GetValue(value, false);
        }

        internal JsValue GetValue(object value, bool returnReferenceToPool)
        {
            if (value is JsValue)
            {
                return value as JsValue;
            }

            if (!(value is Reference))
            {
                return ((Completion) value).Value;
            }

            var reference = value as Reference;
            
            if (reference._baseValue._type == Types.Undefined)
            {
                JsValue val = default(JsValue);
                if (_referenceResolver != null &&
                    _referenceResolver.TryUnresolvableReference(this, reference, out val))
                {
                    return val;
                }
                ExceptionHelper.ThrowReferenceError(this, reference.GetReferencedName() + " is not defined");
            }

            var baseValue = reference._baseValue;

            if (reference.IsPropertyReference())
            {
                if (_referenceResolver != null &&
                    _referenceResolver.TryPropertyReference(this, reference, ref baseValue))
                {
                    return baseValue;
                }

                var referencedName = reference._name;
                if (returnReferenceToPool)
                {
                    _referencePool.Return(reference);
                }
                if (!(reference._baseValue._type != Types.Object && reference._baseValue._type != Types.None))
                {
                    var o = TypeConverter.ToObject(this, baseValue);
                    var v = o.Get(referencedName);
                    return v;
                }
                else
                {
                    var o = TypeConverter.ToObject(this, baseValue);
                    var desc = o.GetProperty(referencedName);
                    if (desc == PropertyDescriptor.Undefined)
                    {
                        return JsValue.Undefined;
                    }

                    if (desc.IsDataDescriptor())
                    {
                        return desc.Value;
                    }

                    var getter = desc.Get;
                    if (getter.IsUndefined())
                    {
                        return Undefined.Instance;
                    }

                    var callable = (ICallable)getter.AsObject();
                    return callable.Call(baseValue, Arguments.Empty);
                }
            }

            var record = (EnvironmentRecord) baseValue;
            if (ReferenceEquals(record, null))
            {
                ExceptionHelper.ThrowArgumentException();
            }

            var bindingValue = record.GetBindingValue(reference._name, reference._strict);

            if (returnReferenceToPool)
            {
                _referencePool.Return(reference);
            }

            return bindingValue;
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-8.7.2
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="value"></param>
        public void PutValue(Reference reference, JsValue value)
        {
            if (reference._baseValue._type == Types.Undefined)
            {
                if (reference._strict)
                {
                    ExceptionHelper.ThrowReferenceError(this);
                }

                Global.Put(reference._name, value, false);
            }
            else if (reference.IsPropertyReference())
            {
                var baseValue = reference._baseValue;
                if (reference._baseValue._type == Types.Object || reference._baseValue._type == Types.None)
                {
                    ((ObjectInstance) baseValue).Put(reference._name, value, reference._strict);
                }
                else
                {
                    PutPrimitiveBase(baseValue, reference._name, value, reference._strict);
                }
            }
            else
            {
                var baseValue = reference._baseValue;
                ((EnvironmentRecord) baseValue).SetMutableBinding(reference._name, value, reference._strict);
            }
        }

        /// <summary>
        /// Used by PutValue when the reference has a primitive base value
        /// </summary>
        public void PutPrimitiveBase(JsValue b, string name, JsValue value, bool throwOnError)
        {
            var o = TypeConverter.ToObject(this, b);
            if (!o.CanPut(name))
            {
                if (throwOnError)
                {
                    ExceptionHelper.ThrowTypeError(this);
                }
                return;
            }

            var ownDesc = o.GetOwnProperty(name);

            if (ownDesc.IsDataDescriptor())
            {
                if (throwOnError)
                {
                    ExceptionHelper.ThrowTypeError(this);
                }
                return;
            }

            var desc = o.GetProperty(name);

            if (desc.IsAccessorDescriptor())
            {
                var setter = (ICallable)desc.Set.AsObject();
                setter.Call(b, new[] { value });
            }
            else
            {
                if (throwOnError)
                {
                    ExceptionHelper.ThrowTypeError(this);
                }
            }
        }

        /// <summary>
        /// Invoke the current value as function.
        /// </summary>
        /// <param name="propertyName">The name of the function to call.</param>
        /// <param name="arguments">The arguments of the function call.</param>
        /// <returns>The value returned by the function call.</returns>
        public void Invoke(string propertyName, params object[] arguments)
        {
            Invoke(propertyName, null, arguments);
        }

        /// <summary>
        /// Invoke the current value as function.
        /// </summary>
        /// <param name="propertyName">The name of the function to call.</param>
        /// <param name="thisObj">The this value inside the function call.</param>
        /// <param name="arguments">The arguments of the function call.</param>
        /// <returns>The value returned by the function call.</returns>
        public void Invoke(string propertyName, object thisObj, object[] arguments)
        {
            var value = GetValue(propertyName);

            Invoke(value, thisObj, arguments);
        }

        /// <summary>
        /// Invoke the current value as function.
        /// </summary>
        /// <param name="value">The function to call.</param>
        /// <param name="arguments">The arguments of the function call.</param>
        /// <returns>The value returned by the function call.</returns>
        public void Invoke(JsValue value, params object[] arguments)
        {
            Invoke(value, null, arguments);
        }

        /// <summary>
        /// Invoke the current value as function.
        /// </summary>
        /// <param name="value">The function to call.</param>
        /// <param name="thisObj">The this value inside the function call.</param>
        /// <param name="arguments">The arguments of the function call.</param>
        /// <returns>The value returned by the function call.</returns>
        public void Invoke(JsValue value, object thisObj, object[] arguments)
        {
            Call(StateInvoke, new InvokeArgs(value, thisObj, arguments));
        }

        public void StateInvoke(RuntimeState state)
        {
            InvokeArgs args = (InvokeArgs)state.arg;
            var value = args.value;
            var arguments = args.arguments;
            var thisObj = args.thisObject;


            if (state.calleeReturned)
            {
                _jsValueArrayPool.ReturnArray((JsValue[])state.local);
                Return(state.calleeReturnValue);
                return;
            }


            var callable = value as ICallable ?? ExceptionHelper.ThrowArgumentException<ICallable>("Can only invoke functions");

            state.local = _jsValueArrayPool.RentArray(arguments.Length);
            for (int i = 0; i < arguments.Length; ++i)
            {
                ((JsValue[])state.local)[i] = JsValue.FromObject(this, arguments[i]);
            }

            if(callable is ScriptFunctionInstance)
            {
                Call((callable as ScriptFunctionInstance).CallState, new CallArgs(JsValue.FromObject(this, thisObj), (JsValue[])state.local));
                return;
            }
            else
            {
                Return(null);
                return;
            }

        }

        /// <summary>
        /// Gets a named value from the Global scope.
        /// </summary>
        /// <param name="propertyName">The name of the property to return.</param>
        public JsValue GetValue(string propertyName)
        {
            return GetValue(Global, propertyName);
        }

        /// <summary>
        /// Gets the last evaluated <see cref="INode"/>.
        /// </summary>
        public INode GetLastSyntaxNode()
        {
            return _lastSyntaxNode;
        }

        /// <summary>
        /// Gets a named value from the specified scope.
        /// </summary>
        /// <param name="scope">The scope to get the property from.</param>
        /// <param name="propertyName">The name of the property to return.</param>
        public JsValue GetValue(JsValue scope, string propertyName)
        {
            AssertNotNullOrEmpty(nameof(propertyName), propertyName);

            var reference = _referencePool.Rent(scope, propertyName, _isStrict);
            var jsValue = GetValue(reference, false);
            _referencePool.Return(reference);
            return jsValue;
        }

        //  http://www.ecma-international.org/ecma-262/5.1/#sec-10.5
        internal bool DeclarationBindingInstantiation(
            DeclarationBindingType declarationBindingType,
            List<FunctionDeclaration> functionDeclarations,
            List<VariableDeclaration> variableDeclarations,
            FunctionInstance functionInstance,
            JsValue[] arguments)
        {
            var env = ExecutionContext.VariableEnvironment._record;
            bool configurableBindings = declarationBindingType == DeclarationBindingType.EvalCode;
            var strict = StrictModeScope.IsStrictModeCode;

            var der = env as DeclarativeEnvironmentRecord;
            bool canReleaseArgumentsInstance = false;
            if (declarationBindingType == DeclarationBindingType.FunctionCode)
            {
                var argsObj = _argumentsInstancePool.Rent(functionInstance, functionInstance._formalParameters, arguments, env, strict);
                canReleaseArgumentsInstance = true;

                if (!ReferenceEquals(der, null))
                {
                    der.AddFunctionParameters(functionInstance, arguments, argsObj);
                }
                else
                {
                    // slow path
                    var parameters = functionInstance._formalParameters;
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        var argName = parameters[i];
                        var v = i + 1 > arguments.Length ? Undefined.Instance : arguments[i];
                        var argAlreadyDeclared = env.HasBinding(argName);
                        if (!argAlreadyDeclared)
                        {
                            env.CreateMutableBinding(argName, v);
                        }

                        env.SetMutableBinding(argName, v, strict);
                    }
                    env.CreateMutableBinding("arguments", argsObj);
                }
            }

            if (functionDeclarations.Count > 0)
            {
                AddFunctionDeclarations(functionDeclarations, env, configurableBindings, strict);
            }

            if (variableDeclarations.Count == 0)
            {
                return canReleaseArgumentsInstance;
            }

            // process all variable declarations in the current parser scope
            if (!ReferenceEquals(der, null))
            {
                der.AddVariableDeclarations(variableDeclarations);
            }
            else
            {
                // slow path
                var variableDeclarationsCount = variableDeclarations.Count;
                for (var i = 0; i < variableDeclarationsCount; i++)
                {
                    var variableDeclaration = variableDeclarations[i];
                    var declarationsCount = variableDeclaration.Declarations.Count;
                    for (var j = 0; j < declarationsCount; j++)
                    {
                        var d = variableDeclaration.Declarations[j];
                        var dn = ((Identifier) d.Id).Name;
                        var varAlreadyDeclared = env.HasBinding(dn);
                        if (!varAlreadyDeclared)
                        {
                            env.CreateMutableBinding(dn, Undefined.Instance);
                        }
                    }
                }
            }

            return canReleaseArgumentsInstance;
        }

        private void AddFunctionDeclarations(List<FunctionDeclaration> functionDeclarations, EnvironmentRecord env, bool configurableBindings, bool strict)
        {
            var functionDeclarationsCount = functionDeclarations.Count;
            for (var i = 0; i < functionDeclarationsCount; i++)
            {
                var f = functionDeclarations[i];
                var fn = f.Id.Name;
                var fo = Function.CreateFunctionObject(f);
                var funcAlreadyDeclared = env.HasBinding(fn);
                if (!funcAlreadyDeclared)
                {
                    env.CreateMutableBinding(fn, configurableBindings);
                }
                else
                {
                    if (ReferenceEquals(env, GlobalEnvironment._record))
                    {
                        var go = Global;
                        var existingProp = go.GetProperty(fn);
                        if (existingProp.Configurable)
                        {
                            var flags = PropertyFlag.Writable | PropertyFlag.Enumerable;
                            if (configurableBindings)
                            {
                                flags |= PropertyFlag.Configurable;
                            }

                            var descriptor = new PropertyDescriptor(Undefined.Instance, flags);
                            go.DefineOwnProperty(fn, descriptor, true);
                        }
                        else
                        {
                            if (existingProp.IsAccessorDescriptor() || !existingProp.Enumerable)
                            {
                                ExceptionHelper.ThrowTypeError(this);
                            }
                        }
                    }
                }

                env.SetMutableBinding(fn, fo, strict);
            }
        }

        internal void UpdateLexicalEnvironment(LexicalEnvironment newEnv)
        {
            _executionContexts.ReplaceTopLexicalEnvironment(newEnv);
        }

        private static void AssertNotNullOrEmpty(string propertyname, string propertyValue)
        {
            if (string.IsNullOrEmpty(propertyValue))
            {
                ExceptionHelper.ThrowArgumentException(propertyname);
            }
        }
    }
}
