using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jint;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceJS.Api.Console;
using SpaceJS.Api.Event;
using SpaceJS.Api.Blocks;

namespace SpaceJS
{
    public class CustomEngine : Engine
    {
        public ConsoleInstance Console;
        public EventInstance Event;
        public BlocksInstance Blocks;

        public CustomEngine(Action<Options> options, Block block) : base(options)
        {
            // Initialize all SpaceJS objects into engine

            Console = ConsoleInstance.CreateObject(this);
            Console.Configure(block);

            Event = EventInstance.CreateObject(this);
            Event.Configure(block);

            Blocks = BlocksInstance.CreateObject(this);
            Blocks.Configure(block);

            // Setup Globals

            Global.FastAddProperty("console", Console, true, false, true);
            Global.FastAddProperty("Event", Event, true, false, true);
            Global.FastAddProperty("Blocks", Blocks, true, false, true);

        }

        public void CustomActionGetter(List<IMyTerminalAction> actions)
        {
            Event.AddActions(actions);
        }
    }
}
