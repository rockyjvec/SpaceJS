using VRage.Game.Components;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using Sandbox.Game.Entities.Character;
using VRage.Game;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using VRage.Game.GUI.TextPanel;
using Sandbox.Game.Entities;
using Jint;
using System;
using Jint.Runtime.Debugger;

namespace rockyjvec.SpaceJS
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class SpaceJS : MySessionComponentBase
    {

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            try {
                var engine = new Engine(options => options.DebugMode(false));

                engine.Execute(@"
                  function hello() { 
                    console.log('Hello World');
                  };
                  
                  hello();
                ");
                
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowMessage("SpaceJS", e.ToString());
            }
        }
    }
}

