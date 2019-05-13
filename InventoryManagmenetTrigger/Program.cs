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
        IMyBlockGroup triggerBlocks;

        IMyTextPanel outputPanel;

        bool curState;

        public Program()
        {
            if (Me.CustomData == null)
            {
                Echo("Put the Search name in custom data of this block.  Script will search for the value in the custom data and that value with trigger.  eg. [full] and [full trigger] and lcd trigger [full]");
                return;
            }


            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            CargoContainers = new List<IMyCargoContainer>();
            List<IMyCargoContainer> invBlocks = new List<IMyCargoContainer>();
            //GridTerminalSystem.GetBlocks(invBlocks);
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();

            GridTerminalSystem.GetBlocks(blocks);

            IMyCargoContainer box;
            IMyTextPanel outPanel;
            for (int i = 0; i < blocks.Count; i++)
            {
                box = blocks[i] as IMyCargoContainer;
                if (box != null)
                {
                    if (box.CustomName.ToLower().Contains($"[{Me.CustomData.ToLower()}]")) CargoContainers.Add(box);
                }
                outPanel = blocks[i] as IMyTextPanel;
                if (outPanel != null && outPanel.CustomName.ToLower() == $"lcd trigger [{Me.CustomData.ToLower()}]")
                {
                    outputPanel = outPanel;
                    outputPanel.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    outputPanel.FontSize = 1.5f;
                }
            }

            //[full trig]
            List<IMyBlockGroup> blockGroups = new List<IMyBlockGroup>();
            GridTerminalSystem.GetBlockGroups(blockGroups);
            for (int i = 0; i < blockGroups.Count; i++)
            {
                if (blockGroups[i].Name.ToLower().Contains($"[{Me.CustomData.ToLower()} trigger]")) triggerBlocks = blockGroups[i];
            }

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

            if (curState) scriptstatus.AppendLine(triggerBlocks.Name + ": Enabled");
            else scriptstatus.AppendLine(triggerBlocks.Name + ": Disabled");
            if (wasFullFlag) scriptstatus.AppendLine("Waiting To Empty");
            else scriptstatus.AppendLine("Filling");

            if (outputPanel == null) Echo(scriptstatus.ToString());
            else outputPanel.WriteText(scriptstatus.ToString());
        }

        public void SetGroup(bool state)
        {
            curState = state;
            IMyFunctionalBlock func;
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            triggerBlocks.GetBlocks(blocks);
            foreach (var b in blocks)
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