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

namespace SpaceJS.Api.Event
{
    public sealed class EventInstance : ObjectInstance
    {
        private Block block;

        private Dictionary<string, List<object>> actions = new Dictionary<string, List<object>>();

        private EventInstance(Jint.Engine engine) : base(engine, "event")
        {
        }

        public void AddActions(List<IMyTerminalAction> actions)
        {
            if(this.actions.Count > 0)
            {
                foreach(string key in this.actions.Keys)
                {
                    var action = MyAPIGateway.TerminalControls.CreateAction<IMyProgrammableBlock>(key);

                    action.Action = (block) => {
                        TriggerAction(key);
                    };
                    
                    action.Icon = @"Textures\GUI\Icons\Actions\Start.dds";
                    action.Name = new StringBuilder(key);

                    actions.Add(action);
                }
            }
        }

        public static EventInstance CreateEventObject(Jint.Engine engine)
        {
            var e = new EventInstance(engine);
            e.Extensible = true;
            e.Prototype = engine.Object.PrototypeObject;

            return e;
        }

        public void Configure(Block block)
        {
            this.block = block;

            FastAddProperty("onAction", new ClrFunctionInstance(Engine, "onAction", OnAction), true, false, true);
            FastAddProperty("offAction", new ClrFunctionInstance(Engine, "offAction", OffAction), true, false, true);
        }

        // Trigger an action
        public void TriggerAction(string actionName)
        {
            if (!actions.ContainsKey(actionName))
            {
                return;
            }

            actions[actionName].ForEach(o =>
            {
                try
                {
                    _engine.Invoke((JsValue)o, actionName);
                }
                catch (Exception e)
                {

                }
            });
        }

        // Add an event listener
        private JsValue OnAction(JsValue thisObject, JsValue[] arguments)
        {
            if (arguments.Length < 2)
            {
                return false;
            }

            var actionName = TypeConverter.ToString(arguments.At(0));

            if (actionName == "" || actionName == null) return false;

            if(actions.Count + 1 > Settings.maxEventActions)
            {
                throw new JavaScriptException("Max number of action events (" + Settings.maxEventActions + ") exceeded.");
            }

            if (!actions.ContainsKey(actionName))
            {
                actions[actionName] = new List<object>();
            }

            actions[actionName].Add(arguments[1]);

            return true;
        }

        // Remove an event listener
        private JsValue OffAction(JsValue thisObject, JsValue[] arguments)
        {
            if (arguments.Length < 2)
            {
                return false;
            }

            var actionName = TypeConverter.ToString(arguments.At(0));

            if (actionName == "" || actionName == null) return false;

            if (!actions.ContainsKey(actionName))
            {
                return false;
            }

            try
            {
                actions[actionName].Remove(arguments.At(1));
                if(actions[actionName].Count == 0)
                {
                    actions.Remove(actionName);
                }
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }
    }
}
