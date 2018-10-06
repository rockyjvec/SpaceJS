using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jint;
using SpaceJS.Api.Console;

namespace SpaceJS
{
    public class Engine
    {
        public static Jint.Engine Create(Action<Options> options, SpaceJS mod)
        {
            var engine = new Jint.Engine(options);

            // Initialize all SpaceJS objects into engine

            var Console = ConsoleInstance.CreateConsoleObject(engine);
            Console.Configure(mod);

            // Setup Globals

            engine.Global.FastAddProperty("console", Console, true, false, true);

            return engine;
        }
    }
}
