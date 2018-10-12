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
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MyProgrammableBlock), false)]
    public class SpaceJS : MyGameLogicComponent
    {
        private MyObjectBuilder_EntityBase m_objectBuilder = null;
        private IMyTerminalBlock tb;
        private IMyCubeBlock cb;
        private IMyCubeBlock m_parent;

        private Jint.Engine engine = null;
        
        private string CustomInfo = "";

        private static List<SpaceJS> blocks = new List<SpaceJS>();
        
        public static void Step()
        {
            blocks.ForEach(spacejs =>
            {
                spacejs.ExecuteStep();
            });
        }

        public override void Close() 
        { 
            tb.AppendingCustomInfo -= AppendingCustomInfo;
            blocks.Remove(this);
        } 

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            m_objectBuilder = objectBuilder;
            tb = Entity as Sandbox.ModAPI.IMyTerminalBlock;
            cb = Entity as IMyCubeBlock;

            if(cb.BlockDefinition.SubtypeId == "JSSmallProgrammableBlock" || cb.BlockDefinition.SubtypeId == "JSLargeProgrammableBlock")
            {
                Startup();
            }
            else
            {
                base.Init(objectBuilder);
            }
        }

        private void Startup()
        {
            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
            tb.AppendingCustomInfo += AppendingCustomInfo;
           
            MyAPIGateway.TerminalControls.CustomControlGetter += GetCustomControls;

            blocks.Add(this);
        }
        
        public void GetCustomControls(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            var cube = block as IMyCubeBlock;
            if(cube.BlockDefinition.SubtypeId == "JSSmallProgrammableBlock" || cube.BlockDefinition.SubtypeId == "JSLargeProgrammableBlock")
            {
                // Remove PB specific controls
                controls.RemoveAll(x => x.Id == "Edit" || x.Id == "ConsoleCommand" || x.Id == "TerminalRun" || x.Id == "Recompile");
                               
                foreach(var o in controls)
                {
                    if(o.Id == "CustomData")
                    {
                        (o as IMyTerminalControlButton).Title = MyStringId.GetOrCompute("Edit");
                        (o as IMyTerminalControlButton).Tooltip = MyStringId.GetOrCompute("Opens the javascript editor.");
                    }
                }
                
                // Add JS controls
                var run = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyProgrammableBlock>("JSEdit");
                run.Title   = MyStringId.GetOrCompute("Run");
                run.Tooltip   = MyStringId.GetOrCompute("Runs the javascript.");
                run.Action = JSRun;
                run.Visible = (b) => true;
                run.Enabled = (b) => true;
                controls.Add(run);

            }
            else
            {
                // Remove JS specific controls
                controls.RemoveAll(x => x.Id == "JSEdit" || x.Id == "JSConsoleCommand" || x.Id == "JSTerminalRun" || x.Id == "JSRecompile");
            };
        }
        
        void JSRun(IMyTerminalBlock pb)
        {
            UpdateCustomInfo("");
            try 
            {
                engine = null;

                engine = Engine.Create(options => options.DebugMode(false), this);

                engine.Execute(pb.CustomData);
            }
            catch (Exception e)
            {
                AppendCustomInfo(e.ToString());
            }

        }

        public void UpdateCustomInfo(string text)
        {
            CustomInfo = text;

            // Prevent CustomInfo from getting too big
            if (CustomInfo.Length > 1000)
            {
                CustomInfo = CustomInfo.Substring(CustomInfo.Length - 1000);
            }

            tb.RefreshCustomInfo();
            var b = tb as IMyProgrammableBlock;
            b.Enabled = !b.Enabled;
            b.Enabled = !b.Enabled;
        }

        public void AppendCustomInfo(string text)
        {
            CustomInfo += text;

            // Prevent CustomInfo from getting too big
            if (CustomInfo.Length > 1000)
            {
                CustomInfo = CustomInfo.Substring(CustomInfo.Length - 1000);
            }

            tb.RefreshCustomInfo();
            var b = tb as IMyProgrammableBlock;
            b.Enabled = !b.Enabled;
            b.Enabled = !b.Enabled;
        }

        public void AppendingCustomInfo(IMyTerminalBlock pb, StringBuilder str)
        {
            str.Clear();
            str.Append(CustomInfo);
        }

        public void ExecuteStep()
        {
            try
            {
                if(engine != null)
                {
                    // Throttle
                    for (uint i = 0; i < 100 && engine.Step(); i++) ;
                }
            }
            catch (Exception e)
            {
                engine.Clear();
                AppendCustomInfo("Error: " + e + "\n");
            }

            // TODO - Implement events.
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return m_objectBuilder;
        }   
    }
}
