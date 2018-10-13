using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jint;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceJS.Api.Console;
using SpaceJS.Api.Event;

namespace SpaceJS
{
    public class CustomEngine : Engine
    {
        public ConsoleInstance Console;
        public EventInstance Event;

        public CustomEngine(Action<Options> options, Block block) : base(options)
        {
            // Initialize all SpaceJS objects into engine

            Console = ConsoleInstance.CreateConsoleObject(this);
            Console.Configure(block);

            Event = EventInstance.CreateEventObject(this);
            Event.Configure(block);

            // Setup Globals

            Global.FastAddProperty("console", Console, true, false, true);
            Global.FastAddProperty("event", Event, true, false, true);

        }

        public void CustomActionGetter(List<IMyTerminalAction> actions)
        {
            Event.AddActions(actions);
        }
    }
}
