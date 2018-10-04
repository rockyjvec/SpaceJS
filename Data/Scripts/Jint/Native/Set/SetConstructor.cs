using Jint.Native.Array;
using Jint.Native.Function;
using Jint.Native.Object;
using Jint.Native.Symbol;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Descriptors.Specialized;
using Jint.Runtime.Interop;

namespace Jint.Native.Set
{
    public sealed class SetConstructor : FunctionInstance, IConstructor
    {
        private SetConstructor(Engine engine, string name) :  base(engine, name, null, null, false)
        {
        }

        public SetPrototype PrototypeObject { get; private set; }

        public static SetConstructor CreateSetConstructorTemplate(string name, Engine engine)
        {
            var ctr = new SetConstructor(engine, name);
            ctr.Extensible = true;

            // The value of the [[Prototype]] internal property of the Set constructor is the Function prototype object
            ctr.Prototype = engine.Function.PrototypeObject;
            ctr.PrototypeObject = SetPrototype.CreatePrototypeObject(engine, ctr);

            ctr.SetOwnProperty("length", new PropertyDescriptor(0, PropertyFlag.Configurable));
            return ctr;
        }

        public static SetConstructor CreateSetConstructor(Engine engine)
        {
            var obj = CreateSetConstructorTemplate("Set", engine);

            // The initial value of Set.prototype is the Set prototype object
            obj.SetOwnProperty("prototype", new PropertyDescriptor(obj.PrototypeObject, PropertyFlag.AllForbidden));

            obj.SetOwnProperty(GlobalSymbolRegistry.Species._value,
                new GetSetPropertyDescriptor(
                    get: new ClrFunctionInstance(engine, "get [Symbol.species]", Species, 0, PropertyFlag.Configurable),
                    set: Undefined,
                    flags: PropertyFlag.Configurable));

            return obj;
        }

        public void Configure()
        {
        }

        private static JsValue Species(JsValue thisObject, JsValue[] arguments)
        {
            return thisObject;
        }

        public override JsValue Call(JsValue thisObject, JsValue[] arguments)
        {
            if (thisObject.IsUndefined())
            {
                ExceptionHelper.ThrowTypeError(_engine, "Constructor Set requires 'new'");
            }

            return Construct(arguments);
        }

        public ObjectInstance Construct(JsValue[] arguments)
        {
            var instance = new SetInstance(Engine)
            {
                Prototype = PrototypeObject,
                Extensible = true
            };
            if (arguments.Length > 0
                && !arguments[0].IsUndefined()
                && !arguments[0].IsNull())
            {
                var iterator = arguments.At(0).GetIterator();
                if (iterator != null)
                {
                    var setterProperty = instance.GetProperty("add");

                    ICallable adder = null;
                    JsValue setterValue;
                    if (setterProperty == null
                        || !setterProperty.TryGetValue(instance, out setterValue)
                        || (adder = setterValue as ICallable) == null)
                    {
                        ExceptionHelper.ThrowTypeError(_engine, "add must be callable");
                        return null;
                    }

                    var args = _engine._jsValueArrayPool.RentArray(1);
                    try
                    {
                        do
                        {
                            var item = iterator.Next();
                            JsValue done;
                            if (item.TryGetValue("done", out done) && done.AsBoolean())
                            {
                                break;
                            }

                            JsValue currentValue;
                            if (!item.TryGetValue("value", out currentValue))
                            {
                                break;
                            }

                            args[0] = ExtractValueFromIteratorInstance(currentValue);

                            adder.Call(instance, args);
                        } while (true);
                    }
                    catch
                    {
                        iterator.Return();
                        throw;
                    }
                    finally
                    {
                        _engine._jsValueArrayPool.ReturnArray(args);
                    }
                }
            }

            return instance;
        }

        private static JsValue ExtractValueFromIteratorInstance(JsValue jsValue)
        {
            if (jsValue is ArrayInstance)
            {
                var ai = jsValue as ArrayInstance;
                uint index = 0;
                if (ai.GetLength() > 1)
                {
                    index = 1;
                }
                JsValue value;
                ai.TryGetValue(index, out value);
                return value;
            }

            return jsValue;
        }
    }
}
