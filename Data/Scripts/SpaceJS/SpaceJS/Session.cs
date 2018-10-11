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
using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;
using System.Text;
using VRage.ObjectBuilders;
using Esprima.Ast;

namespace SpaceJS
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Session : MySessionComponentBase
    {
        private uint counter = 0;
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
//            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            base.Init(sessionComponent);
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
//            counter++;
  //          if(counter % 50 == 0)
    //        {
                SpaceJS.Step();
      //      }
        }
    }
}
