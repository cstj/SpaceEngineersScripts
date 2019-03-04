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
        //List<IntStore> largeList;
        Dictionary<string, int> largeList;
        Dictionary<string, double> invAmounts;

        public Program()
        {
            //Update every 10 ticks
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            //Setup List of large blocks.
            largeList = new Dictionary<string, int>();
            largeList.Add("Construction", 50000);
            largeList.Add("MetalGrid", 15500);
            largeList.Add("InteriorPlate", 55000);
            largeList.Add("SteelPlate", 300000);
            largeList.Add("Girder", 3500);
            largeList.Add("SmallTube", 26000);
            largeList.Add("LargeTube", 6000);
            largeList.Add("Motor", 16000);
            largeList.Add("Display", 500);
            largeList.Add("BulletproofGlass", 12000);
            largeList.Add("Computer", 6500);
            largeList.Add("Reactor", 10000);
            largeList.Add("Thrust", 16000);
            //Grav Missing
            //Meidcal
            largeList.Add("RadioCommunication", 250);
            //Detector Components
            largeList.Add("Explosives", 500);
            //Solar Cell
            largeList.Add("PowerCell", 2800);
            largeList.Add("SuperConductor", 3000);

            largeList.Add("5p56x45mm", 500);        //Personal Ammo
            largeList.Add("25x184mm", 500);         //Ship Ammo
            largeList.Add("missile200mm", 500);     //Ship Ammo

            /*
            largeList.Add("Construction", 50000);
            largeList.Add("MetalGrid", 15500);
            largeList.Add("Interior", 55000);
            largeList.Add("Steelplate", 300000);
            largeList.Add("Girder", 3500);
            largeList.Add("SmallTube", 26000);
            largeList.Add("LargeTube", 6000);
            largeList.Add("Motor", 16000);
            largeList.Add("Display", 500);
            largeList.Add("Glass", 12000);
            largeList.Add("Computer", 6500);
            largeList.Add("Reactor", 10000);
            largeList.Add("Thrust", 16000);
            //Grav Missing
            //Meidcal
            largeList.Add("Radio", 250);
            //Detector Components
            largeList.Add("Explosives", 500);
            //Solar Cell
            largeList.Add("PowerCell", 2800);
            largeList.Add("SuperConductor", 3000);

            largeList.Add("5p56x45mm", 500);        //Personal Ammo
            largeList.Add("25x184mm", 500);         //Ship Ammo
            largeList.Add("missile200mm", 500);     //Ship Ammo
            */
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
            FindItems();


            //Echo("Getting List of Production Blocks");
            List<IMyTerminalBlock> prod = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyProductionBlock>(prod);

            Echo("Processing Automated Assemblers");
            string status;
            int neededAmt;
            double currentAmt;
            int i;
            for (i = 0; i < prod.Count; i++)
            {
                IMyProductionBlock p = (IMyProductionBlock)prod[i];
                //Echo("Processing " + p.CustomName);
                if (p.CustomName.Contains("[") && p.CustomName.Contains("]"))
                {
                    int sqrStr = p.CustomName.IndexOf('[');
                    int sqrEndStr = p.CustomName.IndexOf(']');
                    string assItem = p.CustomName.Substring(sqrStr + 1, sqrEndStr - (sqrStr + 1));
                   // Echo("Found Item Name: " + assItem + " - Getting Needed amt");

                    //Get Needed Amt and Current Amt
                    if (largeList.TryGetValue(assItem, out neededAmt))
                    {
                        //Echo("Getting Current Amount");
                        if (!invAmounts.TryGetValue(assItem, out currentAmt))
                        {
                            currentAmt = 0;
                        }

                        if (neededAmt > 1000)
                            status = assItem + " " + Math.Round((double)(currentAmt / 1000), 1) + "k/" + (neededAmt / 1000) + "k";
                        else
                            status = assItem + " " + (double)(currentAmt) + "/" + (neededAmt);

                        //If our current amount is less than the needed amount then lets start the assembler
                        if (currentAmt < neededAmt)
                        {
                            status = status + " - ON";
                            p.GetActionWithName("OnOff_On").Apply(p);
                        }
                        else
                        {
                            status = status + " - OFF";
                            p.GetActionWithName("OnOff_Off").Apply(p);
                        }
                        Echo(status);
                    }
                }
            }
        }


        //Finds Items of types we care about
        public void FindItems()
        {
            //Echo("Getting Inventory Items From Grid");
            invAmounts = new Dictionary<string, double>();

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
                            //Echo("Test " + item.Type.TypeId);
                            if (item.Type.TypeId == "MyObjectBuilder_Component" || item.Type.TypeId == "MyObjectBuilder_AmmoMagazine")
                            {
                                if (largeList.ContainsKey(item.Type.SubtypeId))
                                {
                                    return true;
                                }
                            }
                            return false;
                        });
                        //Echo("Found " + items.Count);
                        for (int k = 0; k < items.Count; k++)
                        {
                            MyInventoryItem item = items[k];
                            double amt = 0;
                            if (invAmounts.TryGetValue(item.Type.SubtypeId, out amt))
                            {
                                invAmounts[item.Type.SubtypeId] = amt + (double)item.Amount;
                            }
                            else
                            {
                                invAmounts.Add(item.Type.SubtypeId, (double)item.Amount);
                            }
                        }
                    }
                }
            }
            //Echo("Done Getting Items From Grid");
            /*
            foreach(KeyValuePair<string, double> p in invAmounts)
            {
                Echo(p.Key + " - " + p.Value);
            }
            return counter;
            */
        }
    }
}