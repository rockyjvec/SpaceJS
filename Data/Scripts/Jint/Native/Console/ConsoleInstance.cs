using System;
using Jint.Native.Number;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;
using Sandbox.ModAPI;

namespace Jint.Native.Console
{
    public sealed class ConsoleInstance : ObjectInstance
    {
        private ConsoleInstance(Engine engine) : base(engine, "console")
        {
        }

        public static ConsoleInstance CreateConsoleObject(Engine engine)
        {
            var console = new ConsoleInstance(engine);
            console.Extensible = true;
            console.Prototype = engine.Object.PrototypeObject;

            return console;
        }

        public void Configure()
        {
            FastAddProperty("log", new ClrFunctionInstance(Engine, "log", Log), true, false, true);
        }

        private static JsValue Log(JsValue thisObject, JsValue[] arguments)
        {
            var message = TypeConverter.ToString(arguments.At(0));

            MyAPIGateway.Utilities.ShowMessage("SpaceJS", message);
            
            return message;
        }
    }
}
