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

namespace rockyjvec.SpaceJS
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class SpaceJS : MySessionComponentBase
    {
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            try {
                var engine = new Engine();
        
                engine.Execute(@"
                  function hello() { 
                    console.log('Hello World');
                  };
                  setTimeout(hello, 10000);
                ");
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowMessage("SpaceJS", e.ToString());                
            }
            
            MyAPIGateway.Utilities.ShowMessage("SpaceJS", "Started.");
        }
    }
}

