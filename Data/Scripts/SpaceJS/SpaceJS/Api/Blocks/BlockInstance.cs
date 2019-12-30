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
        private Engine engine;

        private BlockInstance(Jint.Engine engine, IMyTerminalBlock tb) : base(engine, "Block")
        {
            this.tb = tb;
            this.engine = engine;
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
            FastAddProperty("getProperty", new ClrFunctionInstance(Engine, "getProperty", GetProperty), true, false, true);
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

        public JsValue GetProperty(JsValue obj, JsValue[] arguments)
        {
            if (arguments.Length < 1)
            {
                return new JsNull();
            }

            var id = TypeConverter.ToString(arguments.At(0));

            ITerminalProperty termProp = tb.GetProperty(id);
            if (termProp == null)
            {
                List<ITerminalProperty> properties = new List<ITerminalProperty>();
                tb.GetProperties(properties);
                js.AppendCustomInfo("Warning! Invalid property:\n[" + tb.CustomName + "]." + id + ".\n\nValid properties:\n");

                foreach (var property in properties)
                {
                    js.AppendCustomInfo("\t" + property.Id + "\n");
                }
                return new JsNull();
            }
            
            if (termProp.TypeName == "Color")
            {
                var color = TerminalPropertyExtensions.GetValueColor(tb, id);
                return new JsString($"[{color.R},{color.G},{color.B},{color.A}]");
            } else if (termProp.TypeName == "Object") {
                return JsValue.FromObject(this.engine, TerminalPropertyExtensions.GetValue<Object>(tb, id));
            } else if (termProp.TypeName == "Boolean") {
                return new JsBoolean(TerminalPropertyExtensions.GetValueBool(tb, id));
            } else if (termProp.TypeName == "StringBuilder") {
                return new JsString(TerminalPropertyExtensions.GetValue<StringBuilder>(tb, id).ToString());
            } else if (termProp.TypeName == "Single") {
                return new JsNumber(TerminalPropertyExtensions.GetValueFloat(tb, id));
            } else
            {
                js.AppendCustomInfo("ERROR! Unsupported type. Contact the developer. [" + tb.CustomName + "].getProperty(" + id + "): (" + termProp.TypeName + ")\n");
                return new JsNull();
            }
        }
    }
}
