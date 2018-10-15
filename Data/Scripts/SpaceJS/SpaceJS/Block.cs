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

using SpaceJS;

namespace SpaceJS
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MyProgrammableBlock), false,
                                    "JSSmallProgrammableBlock",
                                    "JSLargeProgrammableBlock")]
    public class Block : MyGameLogicComponent
    {
        public IMyTerminalBlock tb;
        public IMyCubeBlock cb;
        private IMyCubeBlock m_parent;

        private CustomEngine engine = null;
        
        private string CustomInfo = "";

        // List of all existing javascript blocks in the game
        private static List<Block> blocks = new List<Block>();

        private uint counter = 0;     


        // LifeCycle methods


        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            tb = Entity as IMyTerminalBlock;
            cb = Entity as IMyCubeBlock;

            tb.AppendingCustomInfo += AppendingCustomInfo;

            blocks.Add(this);

            this.Reset();
        }

        public override void Close()
        {
            tb.AppendingCustomInfo -= AppendingCustomInfo;
            blocks.Remove(this);
        }

        
        // Execution methods
        
        
        public static void Step() // Step the interpreter in each block
        {
            if(blocks.Count > 0)
            {
                uint processedBlocks = 0;
                uint usedSteps = 0;
                var copy = new List<Block>(blocks); // prevent "Collection was modified; enumeration operation may not execute." errors
                copy.ForEach(block =>
                {
                    // Tries to distribute load evenly between all blocks
                    usedSteps += block.ExecuteSteps((Settings.maxStepsPerTick - usedSteps) / ((uint)blocks.Count - processedBlocks));
                    processedBlocks++;
                });
            }
        }
        
        public uint ExecuteSteps(uint numberOfSteps) // Step through the interpreter and handle any events
        {
            if (!tb.IsWorking) return 0; // Check that the block is working/powered

            uint usedSteps = 0;
            try
            {
                if (engine != null)
                {
                    // Throttle
                    for (usedSteps = 0; usedSteps < numberOfSteps && engine.Step(); usedSteps++) ;
                }
            }
            catch (Exception e)
            {
                engine.Clear();
                AppendCustomInfo("Error: " + e.Message + "\n");
            }

            return usedSteps;
        }
        
        public void Reset() // Reset and start running (called by clicking the Run button)
        {
            UpdateCustomInfo("");
            try
            {
                engine = null;

                engine = new CustomEngine(null, this);

                engine.Execute(tb.CustomData);
            }
            catch (Exception e)
            {
                AppendCustomInfo(e.ToString());
            }
        }


        // CustomInfo methods


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

        // Actions

        public void CustomActionGetter(List<IMyTerminalAction> actions)
        {
            if(engine != null)
            {
                engine.CustomActionGetter(actions);
            }
        }
    }
}
