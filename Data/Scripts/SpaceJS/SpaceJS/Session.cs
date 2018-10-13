using VRage.Game.Components;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Game;
using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;
using Esprima.Ast;
using VRage.Collections;

namespace SpaceJS
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Session : MySessionComponentBase
    {
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            MyAPIGateway.TerminalControls.CustomControlGetter += CustomControlGetter;
            MyAPIGateway.TerminalControls.CustomActionGetter += CustomActionGetter;
        }

        public override void UpdateBeforeSimulation()
        {
            Block.Step();
        }

        public void CustomControlGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            var cube = block as IMyCubeBlock;
            if (cube.BlockDefinition.SubtypeId == "JSSmallProgrammableBlock" || cube.BlockDefinition.SubtypeId == "JSLargeProgrammableBlock")
            {
                // Remove PB specific controls
                IMyTerminalControlButton customData = controls.Find(x => x.Id == "CustomData") as IMyTerminalControlButton;

                // Add JS controls
                var edit = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyProgrammableBlock>("JSEdit");
                edit.Title = MyStringId.GetOrCompute("Edit");
                edit.Tooltip = MyStringId.GetOrCompute("Opens the javascript editor.");
                edit.Action = customData.Action;
                edit.Visible = (b) => true;
                edit.Enabled = (b) => true;
                controls.Add(edit);

                var run = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyProgrammableBlock>("JSTerminalRun");
                run.Title = MyStringId.GetOrCompute("Run");
                run.Tooltip = MyStringId.GetOrCompute("Runs the javascript.");
                run.Action = Run;
                run.Visible = (b) => true;
                run.Enabled = (b) => true;
                controls.Add(run);

                controls.RemoveAll(x => x.Id == "Edit" || x.Id == "ConsoleCommand" || x.Id == "TerminalRun" || x.Id == "Recompile" || x.Id == "CustomData");

            }
        }

        public void CustomActionGetter(IMyTerminalBlock block, List<IMyTerminalAction> actions)
        {
            var cube = block as IMyCubeBlock;
            if (cube.BlockDefinition.SubtypeId == "JSSmallProgrammableBlock" || cube.BlockDefinition.SubtypeId == "JSLargeProgrammableBlock")
            {
                // Remove PB specific actions
                actions.RemoveAll(x => x.Id == "RunWithDefaultArgument" || x.Id == "Run");

                var b = block.GameLogic.GetAs<Block>();
                b.CustomActionGetter(actions);
            }
        }

        public void Run(IMyTerminalBlock block)
        {
            Block pb = block.GameLogic.GetAs<Block>();
            pb.Run();
        }

    }
}
