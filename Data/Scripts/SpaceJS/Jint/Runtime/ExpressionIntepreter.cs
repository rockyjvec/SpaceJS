using System;
using System.Collections.Generic;
using Esprima;
using Esprima.Ast;
using Jint.Native;
using Jint.Native.Array;
using Jint.Native.Function;
using Jint.Native.Number;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Descriptors.Specialized;
using Jint.Runtime.Environments;
using Jint.Runtime.Interop;
using Jint.Runtime.References;
using Runtime;

namespace Jint.Runtime
{
    public sealed class ExpressionInterpreter
    {
        private readonly Engine _engine;
        private readonly int _maxRecursionDepth;
        private readonly IReferenceResolver _referenceResolver;

        public ExpressionInterpreter(Engine engine)
        {
            _engine = engine;

            // gather some options as fields for faster checks
            _maxRecursionDepth = engine.Options.MaxRecursionDepth;
            _referenceResolver = engine.Options.ReferenceResolver;
        }

        private void EvaluateExpression(RuntimeState state)
        {
            if(state.calleeReturned)
            {
                Return(state.calleeReturnValue);
                return;
            }
            Call(_engine.EvaluateExpression, state);
            return;
        }

        public void Call(Action<RuntimeState> method, object arg)
        {
            _engine.Call(method, arg);
        }

        public void Return(object o)
        {
            _engine.Return(o);
        }

        public void EvaluateConditionalExpression(RuntimeState state)
        {
            ConditionalExpression conditionalExpression = (ConditionalExpression)state.arg;

            if (state.stage == 0)
            {
                if (state.calleeReturned)
                {
                    state.calleeReturned = false;
                    object lref = state.calleeReturnValue;
                    state.stage = 1;

                    if (TypeConverter.ToBoolean(_engine.GetValue(lref, true)))
                    {
                        Call(_engine.EvaluateExpression, conditionalExpression.Consequent);
                        return;
                    }
                    else
                    {
                        Call(_engine.EvaluateExpression, conditionalExpression.Alternate);
                        return;
                    }
                }
                else
                {
                    Call(_engine.EvaluateExpression, conditionalExpression.Test);
                    return;
                }

            }
            else
            {
                if (state.calleeReturned)
                {
                    Return((JsValue)_engine.GetValue(state.calleeReturnValue));
                    return;
                }
            }
        }

        public void EvaluateAssignmentExpression(RuntimeState state)
        {
            AssignmentExpression assignmentExpression = (AssignmentExpression)state.arg;

            if (state.stage == 0)
            {
                if (state.calleeReturned)
                {
                    state.calleeReturned = false;
                    state.local = state.calleeReturnValue; // state.local = lref
                    state.stage = 1;
                }
                else
                {
                    Call(_engine.EvaluateExpression, (Expression)assignmentExpression.Left);
                    return;
                }
            }
            if (state.stage == 1)
            {
                if (state.calleeReturned)
                {
                    state.calleeReturned = false;
                    state.stage = 2;
                }
                else
                {
                    Call(_engine.EvaluateExpression, assignmentExpression.Right);
                    return;
                }
            }

            // Stage 2

            var lref = state.local as Reference;
            JsValue rval = _engine.GetValue(state.calleeReturnValue, true);

            if (lref == null)
            {
                ExceptionHelper.ThrowReferenceError(_engine);
            }

            if (assignmentExpression.Operator == AssignmentOperator.Assign) // "="
            {
                lref.AssertValid(_engine);

                _engine.PutValue(lref, rval);
                _engine._referencePool.Return(lref);
                Return((JsValue)rval);
                return;
            }

            JsValue lval = _engine.GetValue(lref, false);

            switch (assignmentExpression.Operator)
            {
                case AssignmentOperator.PlusAssign:
                    var lprim = TypeConverter.ToPrimitive(lval);
                    var rprim = TypeConverter.ToPrimitive(rval);
                    if (lprim.IsString() || rprim.IsString())
                    {
                        JsString jsString;
                        if (!(lprim is JsString))
                        {
                            jsString = new JsString.ConcatenatedString(TypeConverter.ToString(lprim));
                        }
                        else
                        {
                            jsString = lprim as JsString;
                        }
                        lval = jsString.Append(rprim);
                    }
                    else
                    {
                        lval = TypeConverter.ToNumber(lprim) + TypeConverter.ToNumber(rprim);
                    }
                    break;

                case AssignmentOperator.MinusAssign:
                    lval = TypeConverter.ToNumber(lval) - TypeConverter.ToNumber(rval);
                    break;

                case AssignmentOperator.TimesAssign:
                    if (lval.IsUndefined() || rval.IsUndefined())
                    {
                        lval = Undefined.Instance;
                    }
                    else
                    {
                        lval = TypeConverter.ToNumber(lval) * TypeConverter.ToNumber(rval);
                    }
                    break;

                case AssignmentOperator.DivideAssign:
                    lval = Divide(lval, rval);
                    break;

                case AssignmentOperator.ModuloAssign:
                    if (lval.IsUndefined() || rval.IsUndefined())
                    {
                        lval = Undefined.Instance;
                    }
                    else
                    {
                        lval = TypeConverter.ToNumber(lval) % TypeConverter.ToNumber(rval);
                    }
                    break;

                case AssignmentOperator.BitwiseAndAssign:
                    lval = TypeConverter.ToInt32(lval) & TypeConverter.ToInt32(rval);
                    break;

                case AssignmentOperator.BitwiseOrAssign:
                    lval = TypeConverter.ToInt32(lval) | TypeConverter.ToInt32(rval);
                    break;

                case AssignmentOperator.BitwiseXOrAssign:
                    lval = TypeConverter.ToInt32(lval) ^ TypeConverter.ToInt32(rval);
                    break;

                case AssignmentOperator.LeftShiftAssign:
                    lval = TypeConverter.ToInt32(lval) << (int)(TypeConverter.ToUint32(rval) & 0x1F);
                    break;

                case AssignmentOperator.RightShiftAssign:
                    lval = TypeConverter.ToInt32(lval) >> (int)(TypeConverter.ToUint32(rval) & 0x1F);
                    break;

                case AssignmentOperator.UnsignedRightShiftAssign:
                    lval = (uint)TypeConverter.ToInt32(lval) >> (int)(TypeConverter.ToUint32(rval) & 0x1F);
                    break;

                default:
                    ExceptionHelper.ThrowNotImplementedException();
                    Return(null);
                    return;
            }

            _engine.PutValue(lref, lval);

            _engine._referencePool.Return(lref);
            Return((JsValue)lval);
            return;
        }

        private JsValue Divide(JsValue lval, JsValue rval)
        {
            if (lval.IsUndefined() || rval.IsUndefined())
            {
                return Undefined.Instance;
            }
            else
            {
                var lN = TypeConverter.ToNumber(lval);
                var rN = TypeConverter.ToNumber(rval);

                if (double.IsNaN(rN) || double.IsNaN(lN))
                {
                    return double.NaN;
                }

                if (double.IsInfinity(lN) && double.IsInfinity(rN))
                {
                    return double.NaN;
                }

                if (double.IsInfinity(lN) && rN == 0)
                {
                    if (NumberInstance.IsNegativeZero(rN))
                    {
                        return -lN;
                    }

                    return lN;
                }

                if (lN == 0 && rN == 0)
                {
                    return double.NaN;
                }

                if (rN == 0)
                {
                    if (NumberInstance.IsNegativeZero(rN))
                    {
                        return lN > 0 ? -double.PositiveInfinity : -double.NegativeInfinity;
                    }

                    return lN > 0 ? double.PositiveInfinity : double.NegativeInfinity;
                }

                return lN / rN;
            }
        }

        public void EvaluateBinaryExpression(RuntimeState state)
        {
            BinaryExpression expression = (BinaryExpression)state.arg;

            if (state.stage == 0)
            {
                if (state.calleeReturned)
                {
                    state.calleeReturned = false;
                    state.local = _engine.GetValue(state.calleeReturnValue, true); // left
                    state.stage = 1;
                }
                else
                {
                    if (expression.Left.Type == Nodes.Literal)
                    {
                        state.local = EvaluateLiteral((Literal)expression.Left);
                        state.stage = 1;
                    }
                    else
                    {
                        Call(_engine.EvaluateExpression, expression.Left);
                        return;
                    }

                }
            }

            if (state.stage == 1)
            {
                if (state.calleeReturned)
                {
                    state.calleeReturned = false;
                    state.calleeReturnValue = _engine.GetValue(state.calleeReturnValue, true); // right
                    state.stage = 2;
                }
                else
                {
                    if (expression.Right.Type == Nodes.Literal)
                    {
                        state.calleeReturnValue = EvaluateLiteral((Literal)expression.Right);
                        state.stage = 2;
                    }
                    else
                    {
                        Call(_engine.EvaluateExpression, expression.Right);
                        return;
                    }

                }

            }

            JsValue left = (JsValue)state.local;
            JsValue right = (JsValue)state.calleeReturnValue;
            JsValue value;

            switch (expression.Operator)
            {
                case BinaryOperator.Plus:
                    var lprim = TypeConverter.ToPrimitive(left);
                    var rprim = TypeConverter.ToPrimitive(right);
                    if (lprim.IsString() || rprim.IsString())
                    {
                        value = TypeConverter.ToString(lprim) + TypeConverter.ToString(rprim);
                    }
                    else
                    {
                        value = TypeConverter.ToNumber(lprim) + TypeConverter.ToNumber(rprim);
                    }
                    break;

                case BinaryOperator.Minus:
                    value = TypeConverter.ToNumber(left) - TypeConverter.ToNumber(right);
                    break;

                case BinaryOperator.Times:
                    if (left.IsUndefined() || right.IsUndefined())
                    {
                        value = Undefined.Instance;
                    }
                    else
                    {
                        value = TypeConverter.ToNumber(left) * TypeConverter.ToNumber(right);
                    }
                    break;

                case BinaryOperator.Divide:
                    value = Divide(left, right);
                    break;

                case BinaryOperator.Modulo:
                    if (left.IsUndefined() || right.IsUndefined())
                    {
                        value = Undefined.Instance;
                    }
                    else
                    {
                        value = TypeConverter.ToNumber(left) % TypeConverter.ToNumber(right);
                    }
                    break;

                case BinaryOperator.Equal:
                    value = Equal(left, right) ? JsBoolean.True : JsBoolean.False;
                    break;

                case BinaryOperator.NotEqual:
                    value = Equal(left, right) ? JsBoolean.False : JsBoolean.True;
                    break;

                case BinaryOperator.Greater:
                    value = Compare(right, left, false);
                    if (value.IsUndefined())
                    {
                        value = false;
                    }
                    break;

                case BinaryOperator.GreaterOrEqual:
                    value = Compare(left, right);
                    if (value.IsUndefined() || ((JsBoolean)value)._value)
                    {
                        value = false;
                    }
                    else
                    {
                        value = true;
                    }
                    break;

                case BinaryOperator.Less:
                    value = Compare(left, right);
                    if (value.IsUndefined())
                    {
                        value = false;
                    }
                    break;

                case BinaryOperator.LessOrEqual:
                    value = Compare(right, left, false);
                    if (value.IsUndefined() || ((JsBoolean)value)._value)
                    {
                        value = false;
                    }
                    else
                    {
                        value = true;
                    }
                    break;

                case BinaryOperator.StrictlyEqual:
                    Return((JsValue)(StrictlyEqual(left, right) ? JsBoolean.True : JsBoolean.False));
                    return;

                case BinaryOperator.StricltyNotEqual:
                    Return((JsValue)(StrictlyEqual(left, right) ? JsBoolean.False : JsBoolean.True));
                    return;

                case BinaryOperator.BitwiseAnd:
                    Return((JsValue)(TypeConverter.ToInt32(left) & TypeConverter.ToInt32(right)));
                    return;

                case BinaryOperator.BitwiseOr:
                    Return((JsValue)(TypeConverter.ToInt32(left) | TypeConverter.ToInt32(right)));
                    return;

                case BinaryOperator.BitwiseXOr:
                    Return((JsValue)(TypeConverter.ToInt32(left) ^ TypeConverter.ToInt32(right)));
                    return;

                case BinaryOperator.LeftShift:
                    Return((JsValue)(TypeConverter.ToInt32(left) << (int)(TypeConverter.ToUint32(right) & 0x1F)));
                    return;

                case BinaryOperator.RightShift:
                    Return((JsValue)(TypeConverter.ToInt32(left) >> (int)(TypeConverter.ToUint32(right) & 0x1F)));
                    return;

                case BinaryOperator.UnsignedRightShift:
                    Return((JsValue)((uint)TypeConverter.ToInt32(left) >> (int)(TypeConverter.ToUint32(right) & 0x1F)));
                    return;

                case BinaryOperator.InstanceOf:
                    var f = right.TryCast<FunctionInstance>();
                    if (ReferenceEquals(f, null))
                    {
                        ExceptionHelper.ThrowTypeError(_engine, "instanceof can only be used with a function object");
                    }
                    value = f.HasInstance(left);
                    break;

                case BinaryOperator.In:
                    if (!right.IsObject())
                    {
                        ExceptionHelper.ThrowTypeError(_engine, "in can only be used with an object");
                    }

                    value = right.AsObject().HasProperty(TypeConverter.ToString(left));
                    break;

                default:
                    ExceptionHelper.ThrowNotImplementedException();
                    Return((JsValue)null);
                    return;
            }

            Return((JsValue)value);
            return;
        }

        public void EvaluateLogicalExpression(RuntimeState state)
        {
            BinaryExpression binaryExpression = (BinaryExpression)state.arg;
            if (state.stage == 0)
            {
                if (state.calleeReturned)
                {
                    state.calleeReturned = false;
                    state.local = _engine.GetValue(state.calleeReturnValue, true);
                    state.stage = 1;
                }
                else
                {
                    Call(_engine.EvaluateExpression, binaryExpression.Left);
                    return;
                }
            }

            if (state.stage == 1)
            {
                if (state.calleeReturned)
                {
                    Return((JsValue)_engine.GetValue(state.calleeReturnValue, true));
                    return;
                }
                else
                {
                    var left = (JsValue)state.local;
                    switch (binaryExpression.Operator)
                    {
                        case BinaryOperator.LogicalAnd:
                            if (!TypeConverter.ToBoolean(left))
                            {
                                Return((JsValue)left);
                                return;
                            }

                            Call(_engine.EvaluateExpression, binaryExpression.Right);
                            return;

                        case BinaryOperator.LogicalOr:
                            if (TypeConverter.ToBoolean(left))
                            {
                                Return((JsValue)left);
                                return;
                            }

                            Call(_engine.EvaluateExpression, binaryExpression.Right);
                            return;

                        default:
                            ExceptionHelper.ThrowNotImplementedException();
                            Return((JsValue)null);
                            return;
                    }
                }
            }
        }

        private static bool Equal(JsValue x, JsValue y)
        {
            if (x._type == y._type)
            {
                return StrictlyEqual(x, y);
            }

            if (x._type == Types.Null && y._type == Types.Undefined)
            {
                return true;
            }

            if (x._type == Types.Undefined && y._type == Types.Null)
            {
                return true;
            }

            if (x._type == Types.Number && y._type == Types.String)
            {
                return Equal(x, TypeConverter.ToNumber(y));
            }

            if (x._type == Types.String && y._type == Types.Number)
            {
                return Equal(TypeConverter.ToNumber(x), y);
            }

            if (x._type == Types.Boolean)
            {
                return Equal(TypeConverter.ToNumber(x), y);
            }

            if (y._type == Types.Boolean)
            {
                return Equal(x, TypeConverter.ToNumber(y));
            }

            if (y._type == Types.Object && (x._type == Types.String || x._type == Types.Number))
            {
                return Equal(x, TypeConverter.ToPrimitive(y));
            }

            if (x._type == Types.Object && (y._type == Types.String || y._type == Types.Number))
            {
                return Equal(TypeConverter.ToPrimitive(x), y);
            }

            return false;
        }

        public static bool StrictlyEqual(JsValue x, JsValue y)
        {
            if (x._type != y._type)
            {
                return false;
            }

            if (x._type == Types.Boolean || x._type == Types.String)
            {
                return x.Equals(y);
            }


            if (x._type >= Types.None && x._type <= Types.Null)
            {
                return true;
            }

            if (x is JsNumber)
            {
                var jsNumber = x as JsNumber;
                var nx = jsNumber._value;
                var ny = ((JsNumber)y)._value;
                return !double.IsNaN(nx) && !double.IsNaN(ny) && nx == ny;
            }

            if (x is IObjectWrapper)
            {
                var xw = x as IObjectWrapper;
                if (!(y is IObjectWrapper))
                {
                    return false;
                }
                var yw = y as IObjectWrapper;
                return Equals(xw.Target, yw.Target);
            }

            return x == y;
        }

        public static bool SameValue(JsValue x, JsValue y)
        {
            var typea = TypeConverter.GetPrimitiveType(x);
            var typeb = TypeConverter.GetPrimitiveType(y);

            if (typea != typeb)
            {
                return false;
            }

            switch (typea)
            {
                case Types.None:
                    return true;
                case Types.Number:
                    var nx = TypeConverter.ToNumber(x);
                    var ny = TypeConverter.ToNumber(y);

                    if (double.IsNaN(nx) && double.IsNaN(ny))
                    {
                        return true;
                    }

                    if (nx == ny)
                    {
                        if (nx == 0)
                        {
                            // +0 !== -0
                            return NumberInstance.IsNegativeZero(nx) == NumberInstance.IsNegativeZero(ny);
                        }

                        return true;
                    }

                    return false;
                case Types.String:
                    return TypeConverter.ToString(x) == TypeConverter.ToString(y);
                case Types.Boolean:
                    return TypeConverter.ToBoolean(x) == TypeConverter.ToBoolean(y);
                default:
                    return x == y;
            }

        }

        public static JsValue Compare(JsValue x, JsValue y, bool leftFirst = true)
        {
            JsValue px, py;
            if (leftFirst)
            {
                px = TypeConverter.ToPrimitive(x, Types.Number);
                py = TypeConverter.ToPrimitive(y, Types.Number);
            }
            else
            {
                py = TypeConverter.ToPrimitive(y, Types.Number);
                px = TypeConverter.ToPrimitive(x, Types.Number);
            }

            var typea = px.Type;
            var typeb = py.Type;

            if (typea != Types.String || typeb != Types.String)
            {
                var nx = TypeConverter.ToNumber(px);
                var ny = TypeConverter.ToNumber(py);

                if (double.IsNaN(nx) || double.IsNaN(ny))
                {
                    return Undefined.Instance;
                }

                if (nx == ny)
                {
                    return false;
                }

                if (double.IsPositiveInfinity(nx))
                {
                    return false;
                }

                if (double.IsPositiveInfinity(ny))
                {
                    return true;
                }

                if (double.IsNegativeInfinity(ny))
                {
                    return false;
                }

                if (double.IsNegativeInfinity(nx))
                {
                    return true;
                }

                return nx < ny;
            }
            else
            {
                return String.CompareOrdinal(TypeConverter.ToString(x), TypeConverter.ToString(y)) < 0;
            }
        }

        public Reference EvaluateIdentifier(Identifier identifier)
        {
            var env = _engine.ExecutionContext.LexicalEnvironment;
            var strict = StrictModeScope.IsStrictModeCode;

            return LexicalEnvironment.GetIdentifierReference(env, identifier.Name, strict);
        }

        public JsValue EvaluateLiteral(Literal literal)
        {
            switch (literal.TokenType)
            {
                case TokenType.BooleanLiteral:
                    // bool is fast enough
                    return literal.NumericValue > 0.0 ? JsBoolean.True : JsBoolean.False;

                case TokenType.NullLiteral:
                    // and so is null
                    return JsValue.Null;

                case TokenType.NumericLiteral:
                    return (JsValue)(literal.CachedValue = literal.CachedValue ?? JsNumber.Create(literal.NumericValue));

                case TokenType.StringLiteral:
                    return (JsValue)(literal.CachedValue = literal.CachedValue ?? JsString.Create((string)literal.Value));

                case TokenType.RegularExpression:
                    // should not cache
                    return _engine.RegExp.Construct((System.Text.RegularExpressions.Regex)literal.Value, literal.Regex.Flags);

                default:
                    // a rare case, above types should cover all
                    return JsValue.FromObject(_engine, literal.Value);
            }
        }

        public void EvaluateObjectExpression(RuntimeState state)
        {
            ObjectExpression objectExpression = (ObjectExpression)state.arg;
            // http://www.ecma-international.org/ecma-262/5.1/#sec-11.1.5

            var propertiesCount = objectExpression.Properties.Count;
            if (state.stage == 0)
            {
                if(state.local == null)
                {
                    state.local = _engine.Object.Construct(propertiesCount);
                }
            }
            var obj = (ObjectInstance)state.local;

            if(state.stage < propertiesCount)
            {
                var property = objectExpression.Properties[(int)state.stage];
                var propName = property.Key.GetKey();
                PropertyDescriptor previous;
                PropertyDescriptor propDesc;


                if (property.Kind == PropertyKind.Init || property.Kind == PropertyKind.Data)
                {
                    if (state.calleeReturned)
                    {
                        if (!obj._properties.TryGetValue(propName, out previous))
                        {
                            previous = PropertyDescriptor.Undefined;
                        }

                        state.calleeReturned = false;
                        var exprValue = state.calleeReturnValue;
                        var propValue = _engine.GetValue(exprValue, true);
                        propDesc = new PropertyDescriptor(propValue, PropertyFlag.ConfigurableEnumerableWritable);
                    }
                    else
                    {
                        Call(_engine.EvaluateExpression, property.Value);
                        return;
                    }
                }
                else
                {
                    if (!obj._properties.TryGetValue(propName, out previous))
                    {
                        previous = PropertyDescriptor.Undefined;
                    }

                    if (property.Kind == PropertyKind.Get || property.Kind == PropertyKind.Set)
                    {
                        var function = property.Value as IFunction;

                        if (function == null)
                        {
                            ExceptionHelper.ThrowSyntaxError(_engine);
                        }

                        ScriptFunctionInstance functionInstance;
                        using (new StrictModeScope(function.Strict))
                        {
                            functionInstance = new ScriptFunctionInstance(
                                _engine,
                                function,
                                _engine.ExecutionContext.LexicalEnvironment,
                                StrictModeScope.IsStrictModeCode
                            );
                        }

                        propDesc = new GetSetPropertyDescriptor(
                            get: property.Kind == PropertyKind.Get ? functionInstance : null,
                            set: property.Kind == PropertyKind.Set ? functionInstance : null,
                            flags: PropertyFlag.Enumerable | PropertyFlag.Configurable);
                    }
                    else
                    {
                        ExceptionHelper.ThrowArgumentOutOfRangeException();
                        Return((JsValue)null);
                        return;
                    }
                }

                if (previous != PropertyDescriptor.Undefined)
                {
                    if (StrictModeScope.IsStrictModeCode && previous.IsDataDescriptor() && propDesc.IsDataDescriptor())
                    {
                        ExceptionHelper.ThrowSyntaxError(_engine);
                    }

                    if (previous.IsDataDescriptor() && propDesc.IsAccessorDescriptor())
                    {
                        ExceptionHelper.ThrowSyntaxError(_engine);
                    }

                    if (previous.IsAccessorDescriptor() && propDesc.IsDataDescriptor())
                    {
                        ExceptionHelper.ThrowSyntaxError(_engine);
                    }

                    if (previous.IsAccessorDescriptor() && propDesc.IsAccessorDescriptor())
                    {
                        if (!ReferenceEquals(propDesc.Set, null) && !ReferenceEquals(previous.Set, null))
                        {
                            ExceptionHelper.ThrowSyntaxError(_engine);
                        }

                        if (!ReferenceEquals(propDesc.Get, null) && !ReferenceEquals(previous.Get, null))
                        {
                            ExceptionHelper.ThrowSyntaxError(_engine);
                        }
                    }

                    obj.DefineOwnProperty(propName, propDesc, false);
                }
                else
                {
                    // do faster direct set
                    obj._properties[propName] = propDesc;
                }
                state.stage++;
            }
            else
            {
                Return((JsValue)obj);
                return;
            }
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-11.2.1
        /// </summary>
        /// <param name="memberExpression"></param>
        /// <returns></returns>
        public void EvaluateMemberExpression(RuntimeState state)
        {
            MemberExpression memberExpression = (MemberExpression)state.arg;

            if (state.stage == 0)
            {
                if (state.calleeReturned)
                {
                    state.calleeReturned = false;
                    state.local = state.calleeReturnValue;
                    state.stage = 1;
                }
                else
                {
                    Call(_engine.EvaluateExpression, memberExpression.Object);
                    return;
                }
            }

            string propertyNameString = "";
            if (state.stage == 1)
            {
                if (state.calleeReturned)
                {
                    state.calleeReturned = false;
                    var propertyNameReference = state.calleeReturnValue;
                    var propertyNameValue = _engine.GetValue(propertyNameReference, true);
                    propertyNameString = TypeConverter.ToString(propertyNameValue);
                    state.stage = 2;
                }
                else
                {
                    if (!memberExpression.Computed) // index accessor ?
                    {
                        // we can take fast path without querying the engine again
                        propertyNameString = ((Identifier)memberExpression.Property).Name;
                        state.stage = 2;
                    }
                    else
                    {
                        Call(_engine.EvaluateExpression, memberExpression.Property);
                        return;
                    }
                }
            }

            // Stage 2


            var baseReference = state.local;
            var baseValue = _engine.GetValue(baseReference, false);

            TypeConverter.CheckObjectCoercible(_engine, baseValue, memberExpression, baseReference);

            if (baseReference is Reference)
            {
                var r = baseReference as Reference;
                _engine._referencePool.Return(r);
            }
            Return((Reference)(_engine._referencePool.Rent(baseValue, propertyNameString, StrictModeScope.IsStrictModeCode)));
            return;
        }

        public JsValue EvaluateFunctionExpression(IFunction functionExpression)
        {
            var funcEnv = LexicalEnvironment.NewDeclarativeEnvironment(_engine, _engine.ExecutionContext.LexicalEnvironment);
            var envRec = (DeclarativeEnvironmentRecord)funcEnv._record;

            var closure = new ScriptFunctionInstance(
                _engine,
                functionExpression,
                funcEnv,
                functionExpression.Strict
                );

            if (!string.IsNullOrEmpty(functionExpression.Id?.Name))
            {
                envRec.CreateMutableBinding(functionExpression.Id.Name, closure);
            }

            return closure;
        }

        public class EvaluateCallExpressionLocal
        {
            public object callee;
            public object arguments;
        };

        public void EvaluateCallExpression(RuntimeState state)
        {
            CallExpression callExpression = (CallExpression)state.arg;
            EvaluateCallExpressionLocal local = null;

            if (state.stage == 0)
            {
                if (state.calleeReturned)
                {
                    state.calleeReturned = false;
                    state.local = local = new EvaluateCallExpressionLocal();
                    local.callee = state.calleeReturnValue;
                    local.arguments = ArrayExt.Empty<JsValue>();
                    state.stage = 1;
                }
                else
                {
                    Call(_engine.EvaluateExpression, callExpression.Callee);
                    return;
                }
            }

            if (state.stage == 1)
            {

                if (local == null) local = (EvaluateCallExpressionLocal)state.local;
                var callee = local.callee;

                // todo: implement as in http://www.ecma-international.org/ecma-262/5.1/#sec-11.2.4

                var arguments = (JsValue[])local.arguments;
                if (callExpression.Arguments.Count > 0)
                {
                    if (state.calleeReturned)
                    {
                        state.calleeReturned = false;
                        var ret = (BuildArgumentsReturnType)state.calleeReturnValue;
                        local.arguments = arguments = ret.arguments;
                    }
                    else
                    {
                        arguments = _engine._jsValueArrayPool.RentArray(callExpression.Arguments.Count);
                        var args = new BuildArgumentsArgsType();
                        args.expressionArguments = callExpression.Arguments;
                        args.targetArray = arguments;
                        Call(BuildArguments, args);
                        return;
                    }

                }

                var func = _engine.GetValue(callee, false);

                var r = callee as Reference;
                if (_maxRecursionDepth >= 0)
                {
                    var stackItem = new CallStackElement(callExpression, func, r?._name ?? "anonymous function");

                    var recursionDepth = _engine.CallStack.Push(stackItem);

                    if (recursionDepth > _maxRecursionDepth)
                    {
                        _engine.CallStack.Pop();
                        ExceptionHelper.ThrowRecursionDepthOverflowException(_engine.CallStack, stackItem.ToString());
                    }
                }

                if (func._type == Types.Undefined)
                {
                    ExceptionHelper.ThrowTypeError(_engine, r == null ? "" : $"Object has no method '{r.GetReferencedName()}'");
                }

                if (func._type != Types.Object)
                {
                    if (_referenceResolver == null || !_referenceResolver.TryGetCallable(_engine, callee, out func))
                    {
                        ExceptionHelper.ThrowTypeError(_engine,
                            r == null ? "" : $"Property '{r.GetReferencedName()}' of object is not a function");
                    }
                }

                var callable = func as ICallable;

                if (callable == null)
                {
                    ExceptionHelper.ThrowTypeError(_engine);
                }

                var thisObject = Undefined.Instance;
                if (r != null)
                {
                    if (r.IsPropertyReference())
                    {
                        thisObject = r._baseValue;
                    }
                    else
                    {
                        var env = (EnvironmentRecord)r._baseValue;
                        thisObject = env.ImplicitThisValue();
                    }

                    // is it a direct call to eval ? http://www.ecma-international.org/ecma-262/5.1/#sec-15.1.2.1.1
                    /*                if (r._name == "eval" && callable is EvalFunctionInstance)
                                    {
                                        var instance = callable as EvalFunctionInstance;
                                        var value = instance.Call(thisObject, arguments, true);
                                        _engine._referencePool.Return(r);
                                        Return(value);
                                        return;
                                    }*/
                }

                if (callable is ScriptFunctionInstance)
                {
                    state.stage = 2;
                    state.local = arguments;
                    state.local2 = r;

                    _engine.Call((callable as ScriptFunctionInstance).CallState, new CallArgs(thisObject, arguments));
                    return;
                }

                var result = callable.Call(thisObject, arguments);

                if (_maxRecursionDepth >= 0)
                {
                    _engine.CallStack.Pop();
                }

                if (arguments.Length > 0)
                {
                    _engine._jsValueArrayPool.ReturnArray(arguments);
                }

                _engine._referencePool.Return(r);
                Return((JsValue)result);
                return;

            }


            // Stage 2

            if(state.stage == 2)
            {
                if(state.calleeReturned)
                {
                    if (_maxRecursionDepth >= 0)
                    {
                        _engine.CallStack.Pop();
                    }
                    var arguments = (JsValue[])state.local;

                    if (arguments.Length > 0)
                    {
                        _engine._jsValueArrayPool.ReturnArray(arguments);
                    }

                    _engine._referencePool.Return(state.local2 as Reference);
                    Return((JsValue)state.calleeReturnValue);
                    return;

                }
            }
        }

        public void EvaluateSequenceExpression(RuntimeState state)
        {
            SequenceExpression sequenceExpression = (SequenceExpression)state.arg;

            var result = Undefined.Instance;
            if (state.calleeReturned)
            {
                state.calleeReturned = false;
                result = _engine.GetValue(state.calleeReturnValue, true);
            }

            if (state.stage < sequenceExpression.Expressions.Count)
            {
                var expression = sequenceExpression.Expressions[(int)state.stage];
                Call(_engine.EvaluateExpression, expression);
                state.stage++;
                return;
            }
            else
            {
                Return((JsValue)result);
                return;
            }
        }

        public void EvaluateUpdateExpression(RuntimeState state)
        {
            UpdateExpression updateExpression = (UpdateExpression)state.arg;
            if (state.calleeReturned)
            {
                state.calleeReturned = false;
            }
            else
            {
                Call(_engine.EvaluateExpression, updateExpression.Argument);
                return;
            }


            var value = state.calleeReturnValue;

            var r = (Reference)value;
            r.AssertValid(_engine);

            var oldValue = TypeConverter.ToNumber(_engine.GetValue(value, false));
            double newValue = 0;
            if (updateExpression.Operator == UnaryOperator.Increment)
            {
                newValue = oldValue + 1;
            }
            else if (updateExpression.Operator == UnaryOperator.Decrement)
            {
                newValue = oldValue - 1;
            }
            else
            {
                ExceptionHelper.ThrowArgumentException();
            }

            _engine.PutValue(r, newValue);
            _engine._referencePool.Return(r);
            Return((JsValue)(updateExpression.Prefix ? newValue : oldValue));
        }

        public JsValue EvaluateThisExpression(ThisExpression thisExpression)
        {
            return _engine.ExecutionContext.ThisBinding;
        }

        public void EvaluateNewExpression(RuntimeState state)
        {
            NewExpression newExpression = (NewExpression)state.arg;

            if (state.stage == 0)
            {
                if (state.calleeReturned)
                {
                    state.calleeReturned = false;
                    state.stage = 1;
                    var ret = (BuildArgumentsReturnType)state.calleeReturnValue;
                    state.local = ret.arguments;
                }
                else
                {
                    var args = _engine._jsValueArrayPool.RentArray(newExpression.Arguments.Count);
                    var a = new BuildArgumentsArgsType();
                    a.expressionArguments = newExpression.Arguments;
                    a.targetArray = args;
                    state.local = args;
                    Call(BuildArguments, a);
                    return;
                }
            }

            // Stage 1

            if (state.calleeReturned)
            {
                state.calleeReturned = false;
            }
            else
            {
                // todo: optimize by defining a common abstract class or interface
                Call(_engine.EvaluateExpression, newExpression.Callee);
                return;
            }

            // todo: optimize by defining a common abstract class or interface
            var callee = _engine.GetValue(state.calleeReturnValue, true).TryCast<IConstructor>();
            var arguments = (JsValue[])state.local;

            if (callee == null)
            {
                ExceptionHelper.ThrowTypeError(_engine, "The object can't be used as constructor.");
            }

            // construct the new instance using the Function's constructor method
            var instance = callee.Construct(arguments);

            _engine._jsValueArrayPool.ReturnArray(arguments);

            Return((JsValue)instance);
            return;
        }

        public void EvaluateArrayExpression(RuntimeState state)
        {
            ArrayExpression arrayExpression = (ArrayExpression)state.arg;

            var elements = arrayExpression.Elements;
            var count = elements.Count;

            if (state.stage == 0)
            {
                state.local = _engine.Array.ConstructFast((uint)count);
            }

            if (state.stage < count)
            {
                if (state.calleeReturned)
                {
                    state.calleeReturned = false;
                    ((ArrayInstance)(state.local)).SetIndexValue((uint)state.stage, _engine.GetValue(state.calleeReturnValue, true), updateLength: false);
                    state.stage++;
                    return;
                }
                else
                {
                    var expr = elements[(int)state.stage];
                    if (expr != null)
                    {
                        Call(_engine.EvaluateExpression, (Expression)expr);
                        return;
                    }
                    state.stage++;
                    return;
                }
            }
            else
            {
                Return((JsValue)state.local);
                return;
            }
        }

        public void EvaluateUnaryExpression(RuntimeState state)
        {
            UnaryExpression unaryExpression = (UnaryExpression)state.arg;

            if (state.calleeReturned)
            {
                state.calleeReturned = false;
            }
            else
            {
                Call(_engine.EvaluateExpression, unaryExpression.Argument);
                return;
            }
            var value = state.calleeReturnValue;

            switch (unaryExpression.Operator)
            {
                case UnaryOperator.Plus:
                    Return((JsValue)(TypeConverter.ToNumber(_engine.GetValue(value, true))));
                    return;

                case UnaryOperator.Minus:
                    var n = TypeConverter.ToNumber(_engine.GetValue(value, true));
                    Return((JsValue)(double.IsNaN(n) ? double.NaN : n * -1));
                    return;

                case UnaryOperator.BitwiseNot:
                    Return((JsValue)(~TypeConverter.ToInt32(_engine.GetValue(value, true))));
                    return;

                case UnaryOperator.LogicalNot:
                    Return((JsValue)(!TypeConverter.ToBoolean(_engine.GetValue(value, true))));
                    return;

                case UnaryOperator.Delete:
                    var r = value as Reference;
                    if (r == null)
                    {
                        Return((JsValue)(true));
                        return;
                    }
                    if (r.IsUnresolvableReference())
                    {
                        if (r._strict)
                        {
                            ExceptionHelper.ThrowSyntaxError(_engine);
                        }

                        _engine._referencePool.Return(r);
                        Return((JsValue)(true));
                        return;
                    }
                    if (r.IsPropertyReference())
                    {
                        var o = TypeConverter.ToObject(_engine, r.GetBase());
                        var jsValue = o.Delete(r._name, r._strict);
                        _engine._referencePool.Return(r);
                        Return((JsValue)(jsValue));
                        return;
                    }
                    if (r._strict)
                    {
                        ExceptionHelper.ThrowSyntaxError(_engine);
                    }

                    var bindings = r.GetBase().TryCast<EnvironmentRecord>();
                    var referencedName = r.GetReferencedName();
                    _engine._referencePool.Return(r);

                    Return((JsValue)(bindings.DeleteBinding(referencedName)));
                    return;

                case UnaryOperator.Void:
                    _engine.GetValue(value, true);
                    Return((JsValue)(Undefined.Instance));
                    return;

                case UnaryOperator.TypeOf:
                    r = value as Reference;
                    if (r != null)
                    {
                        if (r.IsUnresolvableReference())
                        {
                            _engine._referencePool.Return(r);
                            Return((JsValue)("undefined"));
                            return;
                        }
                    }

                    var v = _engine.GetValue(value, true);

                    if (v.IsUndefined())
                    {
                        Return((JsValue)("undefined"));
                        return;
                    }
                    if (v.IsNull())
                    {
                        Return((JsValue)("object"));
                        return;
                    }
                    switch (v.Type)
                    {
                        case Types.Boolean: Return((JsValue)("boolean")); return;
                        case Types.Number: Return((JsValue)("number")); return;
                        case Types.String: Return((JsValue)("string")); return;
                    }
                    if (v.TryCast<ICallable>() != null)
                    {
                        Return((JsValue)("function"));
                        return;
                    }
                    Return((JsValue)("object"));
                    return;

                default:
                    ExceptionHelper.ThrowArgumentException();
                    Return((JsValue)(null));
                    return;
            }
        }

        public class BuildArgumentsArgsType
        {
            public List<ArgumentListElement> expressionArguments;
            public JsValue[] targetArray;
        }
        public class BuildArgumentsReturnType
        {
            public JsValue[] arguments;
        }
        public void BuildArguments(RuntimeState state)
        {
            BuildArgumentsArgsType args = (BuildArgumentsArgsType)state.arg;

            List<ArgumentListElement> expressionArguments = args.expressionArguments;

            state.local = true;
            if (state.stage < args.targetArray.Length)
            {
                if (state.calleeReturned)
                {
                    var argument = (Expression)expressionArguments[(int)state.stage];
                    args.targetArray[(int)state.stage] = _engine.GetValue(state.calleeReturnValue, true);
                    state.calleeReturned = false;
                    state.stage++;
                    return;
                }
                else
                {
                    var argument = (Expression)expressionArguments[(int)state.stage];
                    Call(_engine.EvaluateExpression, argument);
                    return;
                }

            }
            else
            {
                var ret = new BuildArgumentsReturnType();
                ret.arguments = args.targetArray;
                string ar = "";
                for (int i = 0; i < args.targetArray.Length; i++)
                    ar += ", " + args.targetArray[i];

                Return(ret);
                return;
            }
        }
    }
}
