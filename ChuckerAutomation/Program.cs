using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
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
        string MainType;
        string MainSubType;
        double maxAmount;
        public Program()
        {
            MainType = "Ingot";
            MainSubType = "Stone";
            maxAmount = 10000;
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Echo("Getting Inventory Numbers");
            double amt = FindItems(MainType, MainSubType);
            Echo("Found " + MainType + ":" + MainSubType + " - " + amt);
            //Find the connector!
            List<IMyTerminalBlock> sorters = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyConveyorSorter>(sorters);

            Echo("Looking for " + "Throw:" + MainType + ":" + MainSubType);
            string mTU = MainType.ToUpper();
            string mSTU = MainSubType.ToUpper();
            for (int i = 0; i < sorters.Count; i++)
            {
                IMyConveyorSorter c = sorters[i] as IMyConveyorSorter;
                if (c.CustomName.ToUpper().Contains("THROW:" + mTU + ":" + mSTU))
                {
                    Echo("Found Sorter: " + c.CustomName);
                    if (amt > maxAmount)
                    {
                        Echo("Enabling Sorter " + amt + "/" + maxAmount);
                        if (c.Enabled == false) c.Enabled = true;
                        if (c.DrainAll == false) c.DrainAll = true;
                    }
                    else
                    {
                        Echo("Disabling Sorter " + amt + "/" + maxAmount);
                        if (c.Enabled == true) c.Enabled = false;
                        if (c.DrainAll == true) c.DrainAll = false;
                    }
                }
            }
        }


        public double FindItems(string type, string subtype)
        {
            //Echo("Getting Inventory Items From Grid");
            double invAmount = 0;

            List<IMyTerminalBlock> invBlocks = new List<IMyTerminalBlock>();
            //GridTerminalSystem.GetBlocks(invBlocks);
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(invBlocks);

            for (int i = 0; i < invBlocks.Count; i++)
            {
                IMyTerminalBlock blk = invBlocks[i];
                for (int iCnt = 0; iCnt < blk.InventoryCount; iCnt++)
                {
                    //Echo("Processing " + blk.CustomName + " inv " + iCnt);
                    IMyInventory inv = blk.GetInventory(iCnt);
                    if (inv != null)
                    {

                        List<MyInventoryItem> items = new List<MyInventoryItem>();
                        inv.GetItems(items, item =>
                        {
                            //if (item.Type.TypeId.ToUpper().Contains("INGOT")) Echo(item.Type.TypeId + ":" + item.Type.SubtypeId);
                            //MyObjectBuilder_
                            if (item.Type.TypeId == "MyObjectBuilder_" + type && item.Type.SubtypeId == subtype)
                                return true;
                            return false;
                        });
                        //Echo("Found " + items.Count);
                        for (int k = 0; k < items.Count; k++)
                        {
                            MyInventoryItem item = items[k];
                            invAmount += (double)item.Amount;
                        }
                    }
                }
            }
            return invAmount;
        }
    }
}