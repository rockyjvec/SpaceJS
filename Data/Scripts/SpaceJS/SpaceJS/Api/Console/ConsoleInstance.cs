using System;
using Jint.Native.Number;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;
using Sandbox.ModAPI;
using Jint;
using Jint.Native;

namespace SpaceJS.Api.Console
{
    public sealed class ConsoleInstance : ObjectInstance
    {
        private Block block;

        private ConsoleInstance(Jint.Engine engine) : base(engine, "console")
        {
        }

        public static ConsoleInstance CreateConsoleObject(Jint.Engine engine)
        {
            var console = new ConsoleInstance(engine);
            console.Extensible = true;
            console.Prototype = engine.Object.PrototypeObject;

            return console;
        }

        public void Configure(Block block)
        {
            this.block = block;

            FastAddProperty("log", new ClrFunctionInstance(Engine, "log", Log), true, false, true);
        }

        // Write a log entry to the PB custom info
        private JsValue Log(JsValue thisObject, JsValue[] arguments)
        {
            var message = TypeConverter.ToString(arguments.At(0));

            block.AppendCustomInfo(message + "\n");
            
            return message;
        }
    }
}
