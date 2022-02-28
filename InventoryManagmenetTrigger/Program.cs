using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        List<IMyCargoContainer> CargoContainers;
        bool wasFullFlag;
        List<IMyFunctionalBlock> triggerBlocks;
        string groupName = null;

        IMyTextPanel outputPanel = null;

        bool curState;

        public Program()
        {
            groupName = Me.CustomData;
            if (Me.CustomData == null || Me.CustomData == "")
            {
                Echo("Put the Search name in custom data of this block.  Script will search for the value in the custom data and that value with trigger.  eg. [full] and [full trigger] and lcd trigger [full]");
                return;
            }
            
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(groupName);
            CargoContainers = new List<IMyCargoContainer>();
            triggerBlocks = new List<IMyFunctionalBlock>();
            List<IMyCargoContainer> invBlocks = new List<IMyCargoContainer>();
            //GridTerminalSystem.GetBlocks(invBlocks);
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            
            
            GridTerminalSystem.GetBlocks(blocks);

            IMyCargoContainer box;
            IMyTextPanel outPanel;
            IMyFunctionalBlock funcBlock;
            for (int i = 0; i < blocks.Count; i++)
            {
                box = blocks[i] as IMyCargoContainer;
                if (box != null && box.CustomName.ToLower().Contains($"[{groupName.ToLower()}]"))
                {
                    sb.AppendLine("Found Box " + box.CustomName);
                    CargoContainers.Add(box);
                }

                outPanel = blocks[i] as IMyTextPanel;
                if (outPanel != null && outPanel.CustomName.ToLower() == $"lcd trigger [{groupName.ToLower()}]")
                {
                    outputPanel = outPanel;
                    outputPanel.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    outputPanel.FontSize = 1.5f;
                    sb.AppendLine("Found LCD " + outPanel.CustomName);
                }
                funcBlock = blocks[i] as IMyFunctionalBlock;
                if (funcBlock != null && funcBlock.CustomName.ToLower().Contains($"[{groupName.ToLower()} trigger]"))
                {
                    sb.AppendLine("Found Triggered Block " + funcBlock.CustomName);
                    triggerBlocks.Add(funcBlock);
                }
            }
            
            sb.AppendLine("Box Count " + CargoContainers.Count);
            sb.AppendLine("Triggered Blocks Count " + triggerBlocks.Count);
            Echo(sb.ToString());
            wasFullFlag = false;
            curState = false;

            //Me.GetSurface(0).ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
            //Me.GetSurface(0).Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
            
        }

        public void Save()
        {
            
        }

        public void Main(string argument, UpdateType updateSource)
        {
            StringBuilder scriptstatus = new StringBuilder();
            scriptstatus.AppendLine(groupName);
            MyFixedPoint totalVolume = 0;
            MyFixedPoint usedVolume = 0;
            IMyInventory thisInv;
            scriptstatus.AppendLine("Found " + CargoContainers.Count + " containers");
            foreach(var b in CargoContainers)
            {
                for(int i = 0; i < b.InventoryCount; i++)
                {
                    thisInv = b.GetInventory(i);
                    usedVolume += thisInv.CurrentVolume;
                    totalVolume += thisInv.MaxVolume;
                }
            }

            
            var fullPercent = Math.Round(((double)usedVolume / (double)totalVolume), 4);
            scriptstatus.AppendLine("Percentage: " + fullPercent * 100);
            if (fullPercent > .95)
            {
                if (wasFullFlag == false)
                {
                    SetGroup(false);
                    wasFullFlag = true;
                }
            }
            else if (fullPercent < .7 && curState == false)
            {
                SetGroup(true);
                wasFullFlag = false;
            }
            else if (wasFullFlag == false && curState == false)
            {
                SetGroup(true);
                wasFullFlag = false;
            }

            if (triggerBlocks.Count == 0)
            {
                scriptstatus.AppendLine($"Add [{groupName} trigger] to a block to have it triggered");
            }
            else
            {
                if (curState) scriptstatus.AppendLine(groupName + ": Enabled");
                else scriptstatus.AppendLine(groupName + ": Disabled");
            }
            //if (wasFullFlag) scriptstatus.AppendLine("Waiting To Empty");
            //else scriptstatus.AppendLine("Filling");
            
            if (outputPanel == null) Echo(scriptstatus.ToString());
            else outputPanel.WriteText(scriptstatus.ToString());
        }
        
        public void SetGroup(bool state)
        {
            if (triggerBlocks.Count  > 0)
            { 
                curState = state;
                IMyFunctionalBlock func;
                foreach (var b in triggerBlocks)
                {
                    func = b as IMyFunctionalBlock;
                    if (func != null)
                    {
                        func.Enabled = state;
                    }
                }
            }
        }
    }
}