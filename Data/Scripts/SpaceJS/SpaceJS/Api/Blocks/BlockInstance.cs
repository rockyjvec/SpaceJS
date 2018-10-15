using System;
using System.Collections.Generic;
using Jint.Native.Number;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;
using Sandbox.ModAPI;
using Jint;
using Jint.Native;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;
using System.Text;
using Sandbox.ModAPI.Interfaces;

namespace SpaceJS.Api.Blocks
{
    public sealed class BlockInstance : ObjectInstance
    {
        private SpaceJS.Block js;
        private IMyTerminalBlock tb;

        private BlockInstance(Jint.Engine engine, IMyTerminalBlock tb) : base(engine, "Block")
        {
            this.tb = tb;
        }

        public static BlockInstance CreateObject(Jint.Engine engine, IMyTerminalBlock tb)
        {
            var e = new BlockInstance(engine, tb);
            e.Extensible = true;
            e.Prototype = engine.Object.PrototypeObject;

            return e;
        }

        public void Configure(SpaceJS.Block js)
        {
            this.js = js;

            FastAddProperty("applyAction", new ClrFunctionInstance(Engine, "applyAction", ApplyAction), true, false, true);
        }

        public JsValue ApplyAction(JsValue obj, JsValue[] arguments)
        {
            if (arguments.Length < 1)
                return false;
            var name = TypeConverter.ToString(arguments.At(0));

            ITerminalAction action = tb.GetActionWithName(name);
            if (action == null)
                return false;

            // TODO - handle parameters

            action.Apply(tb);
            return true;
        }
    }
}
