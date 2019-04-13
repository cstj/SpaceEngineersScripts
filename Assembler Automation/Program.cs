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
        Dictionary<string, int> largeList;
        Dictionary<string, double> invAmounts;
        Dictionary<string, string> TypeToBlueprint;
        IMyCargoContainer ingotDump = null;
        double defaultQueueAmount = 20f;

        public struct AutoAsselber
        {
            public AutoAsselber(IMyProductionBlock myProduction, Dictionary<string, AutoQueueItem> queue)
            {
                Assembler = myProduction;
                Queue = queue;
            }

            public IMyProductionBlock Assembler { get; set; }
            public Dictionary<string, AutoQueueItem> Queue { get; set; }
        }

        public struct AutoQueueItem
        {
            public double Amount { get; set; }
            public int QueueIndex { get; set; }
        }

        public Program()
        {
            //Update every 10 ticks
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            //Runtime.UpdateFrequency = UpdateFrequency.Once;

            //Setup List of large blocks.
            largeList = new Dictionary<string, int>();
            largeList.Add("MyObjectBuilder_BlueprintDefinition/ConstructionComponent", 20000);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/MetalGrid", 5000);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/InteriorPlate", 4000);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/SteelPlate", 30000);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/GirderComponent", 500);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/SmallTube", 4000);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/LargeTube", 2000);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/MotorComponent", 2000);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/Display", 500);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/BulletproofGlass", 1000);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/ComputerComponent", 1000);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/ReactorComponent", 1000);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/ThrustComponent", 5000);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/GravityGeneratorComponent", 100);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/MedicalComponent", 50);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/RadioCommunicationComponent", 200);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/DetectorComponent", 1000);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/ExplosivesComponent", 200);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/SolarCell", 100);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/PowerCell", 500);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/Superconductor", 1000);

            largeList.Add("MyObjectBuilder_BlueprintDefinition/ShieldComponentBP", 1000);

            //Ammo
            largeList.Add("MyObjectBuilder_BlueprintDefinition/NATO_5p56x45mmMagazine", 100);        //Personal Ammo
            largeList.Add("MyObjectBuilder_BlueprintDefinition/NATO_25x184mmMagazine", 500);         //Ship Ammo
            largeList.Add("MyObjectBuilder_BlueprintDefinition/Missile200mm", 300);     //Ship Ammo

            //Hand Tools
            largeList.Add("MyObjectBuilder_BlueprintDefinition/AngleGrinder4", 5);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/HandDrill4", 5);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/Welder4", 5);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/UltimateAutomaticRifle", 5);

            //Items
            largeList.Add("MyObjectBuilder_BlueprintDefinition/HydrogenBottle", 5);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/OxygenBottle", 5);

            TypeToBlueprint = new Dictionary<string, string>
            {
                { "MyObjectBuilder_Component/Construction", "MyObjectBuilder_BlueprintDefinition/ConstructionComponent" },
                { "MyObjectBuilder_Component/MetalGrid", "MyObjectBuilder_BlueprintDefinition/MetalGrid" },
                { "MyObjectBuilder_Component/InteriorPlate", "MyObjectBuilder_BlueprintDefinition/InteriorPlate" },
                { "MyObjectBuilder_Component/SteelPlate", "MyObjectBuilder_BlueprintDefinition/SteelPlate" },
                { "MyObjectBuilder_Component/Girder", "MyObjectBuilder_BlueprintDefinition/GirderComponent" },
                { "MyObjectBuilder_Component/SmallTube", "MyObjectBuilder_BlueprintDefinition/SmallTube" },
                { "MyObjectBuilder_Component/LargeTube", "MyObjectBuilder_BlueprintDefinition/LargeTube" },
                { "MyObjectBuilder_Component/Motor", "MyObjectBuilder_BlueprintDefinition/MotorComponent" },
                { "MyObjectBuilder_Component/Display", "MyObjectBuilder_BlueprintDefinition/Display" },
                { "MyObjectBuilder_Component/BulletproofGlass", "MyObjectBuilder_BlueprintDefinition/BulletproofGlass" },
                { "MyObjectBuilder_Component/Computer", "MyObjectBuilder_BlueprintDefinition/ComputerComponent" },
                { "MyObjectBuilder_Component/Reactor", "MyObjectBuilder_BlueprintDefinition/ReactorComponent" },
                { "MyObjectBuilder_Component/Thrust", "MyObjectBuilder_BlueprintDefinition/ThrustComponent" },
                { "MyObjectBuilder_Component/GravityGenerator", "MyObjectBuilder_BlueprintDefinition/GravityGeneratorComponent" },
                { "MyObjectBuilder_Component/Medical", "MyObjectBuilder_BlueprintDefinition/MedicalComponent" },
                { "MyObjectBuilder_Component/RadioCommunication", "MyObjectBuilder_BlueprintDefinition/RadioCommunicationComponent" },
                { "MyObjectBuilder_Component/Detector", "MyObjectBuilder_BlueprintDefinition/DetectorComponent" },
                { "MyObjectBuilder_Component/Explosives", "MyObjectBuilder_BlueprintDefinition/ExplosivesComponent" },
                { "MyObjectBuilder_Component/SolarCell", "MyObjectBuilder_BlueprintDefinition/SolarCell" },
                { "MyObjectBuilder_Component/PowerCell", "MyObjectBuilder_BlueprintDefinition/PowerCell" },
                { "MyObjectBuilder_Component/Superconductor", "MyObjectBuilder_BlueprintDefinition/Superconductor" },

                { "MyObjectBuilder_AmmoMagazine/NATO_5p56x45mm", "MyObjectBuilder_BlueprintDefinition/NATO_5p56x45mmMagazine" },
                { "MyObjectBuilder_AmmoMagazine/NATO_25x184mm", "MyObjectBuilder_BlueprintDefinition/NATO_25x184mmMagazine" },
                { "MyObjectBuilder_AmmoMagazine/Missile200mm", "MyObjectBuilder_BlueprintDefinition/Missile200mm" },

                { "MyObjectBuilder_PhysicalGunObject/AngleGrinder1Item", "MyObjectBuilder_BlueprintDefinition/AngleGrinder1"},
                { "MyObjectBuilder_PhysicalGunObject/AngleGrinder2Item", "MyObjectBuilder_BlueprintDefinition/AngleGrinder2"},
                { "MyObjectBuilder_PhysicalGunObject/AngleGrinder3Item", "MyObjectBuilder_BlueprintDefinition/AngleGrinder3"},
                { "MyObjectBuilder_PhysicalGunObject/AngleGrinder4Item", "MyObjectBuilder_BlueprintDefinition/AngleGrinder4"},
                { "MyObjectBuilder_PhysicalGunObject/HandDrill1Item", "MyObjectBuilder_BlueprintDefinition/HandDrill1"},
                { "MyObjectBuilder_PhysicalGunObject/HandDrill2Item", "MyObjectBuilder_BlueprintDefinition/HandDrill2"},
                { "MyObjectBuilder_PhysicalGunObject/HandDrill3Item", "MyObjectBuilder_BlueprintDefinition/HandDrill3"},
                { "MyObjectBuilder_PhysicalGunObject/HandDrill4Item", "MyObjectBuilder_BlueprintDefinition/HandDrill4"},
                { "MyObjectBuilder_PhysicalGunObject/Welder1Item", "MyObjectBuilder_BlueprintDefinition/Welder1"},
                { "MyObjectBuilder_PhysicalGunObject/Welder2Item", "MyObjectBuilder_BlueprintDefinition/Welder2"},
                { "MyObjectBuilder_PhysicalGunObject/Welder3Item", "MyObjectBuilder_BlueprintDefinition/Welder3"},
                { "MyObjectBuilder_PhysicalGunObject/Welder4Item", "MyObjectBuilder_BlueprintDefinition/Welder4"},

                { "MyObjectBuilder_PhysicalGunObject/AutomaticRifleItem", "MyObjectBuilder_BlueprintDefinition/AutomaticRifle"},
                { "MyObjectBuilder_PhysicalGunObject/PreciseAutomaticRifleItem", "MyObjectBuilder_BlueprintDefinition/PreciseAutomaticRifle"},
                { "MyObjectBuilder_PhysicalGunObject/RapidFireAutomaticRifleItem", "MyObjectBuilder_BlueprintDefinition/RapidFireAutomaticRifle"},
                { "MyObjectBuilder_PhysicalGunObject/UltimateAutomaticRifleItem", "MyObjectBuilder_BlueprintDefinition/UltimateAutomaticRifle"},
                { "MyObjectBuilder_GasContainerObject/HydrogenBottle", "MyObjectBuilder_BlueprintDefinition/HydrogenBottle"},
                { "MyObjectBuilder_OxygenContainerObject/OxygenBottle", "MyObjectBuilder_BlueprintDefinition/OxygenBottle"},

                { "MyObjectBuilder_Component/ShieldComponent", "MyObjectBuilder_BlueprintDefinition/ShieldComponentBP" },
            };
        }

        public void Save()
        {
        }


        public void Main(string argument, UpdateType updateSource)
        {
            StringBuilder status = new StringBuilder();
            status.AppendLine("Getting Inventory Numbers");
            FindItems();

            status.AppendLine("Getting Queues");
            List<AutoAsselber> autoAssemblers = GetQueuedAmounts();

            status.AppendLine("Cleaning Assemblers");
            //Do at same time as getting queues?
            IMyProductionBlock p;
            for (int i = 0; i < autoAssemblers.Count; i++)
            {
                p = autoAssemblers[i].Assembler;
                ChkAssemblerClog(p);
            }          

            MyDefinitionId blueprint;

            double amountStored = 0;
            double amountQueued = 0;
            AutoQueueItem amountThisQueue;

            status.AppendLine("Processing Queues/Requirements");
            string displayKey;

            //Work out what we need
            if (autoAssemblers.Count > 0)
            {
                status.AppendLine("Detected " + autoAssemblers.Count + " Auto Assemblers");
                status.AppendLine("Will queue " + defaultQueueAmount + " at a time");

                bool handtool = false;
                long counter = 0;

                foreach (KeyValuePair<string, int> req in largeList)
                {
                    //If we're working with hand tools, queue one and queue on 0, use hashset?
                    if (req.Key.StartsWith("MyObjectBuilder_BlueprintDefinition/AngleGrinder") ||
                        req.Key.StartsWith("MyObjectBuilder_BlueprintDefinition/HandDrill") ||
                        req.Key.StartsWith("MyObjectBuilder_BlueprintDefinition/Welder") ||
                        req.Key.EndsWith("AutomaticRifle") ||
                        req.Key.EndsWith("Bottle"))
                    {
                        handtool = true;
                    }
                    else
                    {
                        handtool = false;
                    }

                    //Get Amount in boxes
                    amountStored = 0;
                    invAmounts.TryGetValue(req.Key, out amountStored);
                    amountQueued = 0;
                    counter = 0;
                    string action;

                    if (amountStored < req.Value)
                    {
                        action = "Mak - ";
                        foreach (var autoAsselber in autoAssemblers)
                        {
                            if (!autoAsselber.Queue.TryGetValue(req.Key, out amountThisQueue))
                            {
                                blueprint = MyDefinitionId.Parse(req.Key);
                                if (blueprint != null)
                                {
                                    try
                                    {
                                        if (autoAsselber.Assembler.CanUseBlueprint(blueprint))
                                        {
                                            //Get this item from the assembler queue                                            
                                            amountQueued += amountThisQueue.Amount;

                                            counter = 0;
                                            if (handtool == false)
                                            {
                                                autoAsselber.Assembler.AddQueueItem(blueprint, defaultQueueAmount);
                                                counter += Convert.ToInt32(defaultQueueAmount);
                                            }
                                            else
                                            {
                                                //Queue one and only queue upto the correct amount
                                                if (counter > req.Value || amountStored > req.Value) break;
                                                autoAsselber.Assembler.AddQueueItem(blueprint, 1f);
                                                counter++;
                                            }
                                        }
                                    }
                                    catch { }
                                    amountQueued += counter;
                                }
                            }
                            else
                            {
                                amountQueued += amountThisQueue.Amount;
                            }
                        }
                    }
                    else
                    {
                        action = "Full - ";
                        AutoQueueItem value;
                        foreach (var a in autoAssemblers)
                        {
                            if (a.Queue.TryGetValue(req.Key, out value))
                            {
                                a.Assembler.RemoveQueueItem(value.QueueIndex, value.Amount);
                            }
                        }
                        amountQueued = 0;
                    }

                    //Add Info to statu
                    status.Append(action);
                    displayKey = req.Key.Substring(req.Key.IndexOf('/') + 1);
                    if (displayKey.Length > 15)
                        status.Append(displayKey.Substring(0, 10));
                    else
                        status.Append(displayKey);
                    status.Append(" ");
                    if (amountStored >= 1000)
                        status.Append(Math.Round(amountStored / 1000, 1) + "k");
                    else
                        status.Append(amountStored);
                    status.Append("(" + amountQueued + ")");
                    status.Append("/");
                    if (req.Value >= 1000)
                        status.Append(Math.Round(Convert.ToDouble(req.Value) / 1000, 1) + "k");
                    else
                        status.Append(req.Value);
                    status.AppendLine();
                }
            }
            else
            {
                status.AppendLine("Could not find assembler.  Please put [Auto] in an assembler name.");
            }
            Echo(status.ToString());
        }

        private void ChkAssemblerClog(IMyProductionBlock p)
        {
            if (ingotDump != null)
            {
                var inv = p.GetInventory(0);
                var items = new List<MyInventoryItem>();
                inv.GetItems(items);
                int i = -1;
                VRage.MyFixedPoint maxAmount = 0;
                while (inv.IsItemAt(++i))
                { // set MaxAmount based on what it is.
                    if (items[i].Type.SubtypeId == "Stone") maxAmount = (VRage.MyFixedPoint)10.00;
                    if (items[i].Type.SubtypeId == "Iron") maxAmount = (VRage.MyFixedPoint)600.00;
                    if (items[i].Type.SubtypeId == "Nickel") maxAmount = (VRage.MyFixedPoint)70.00;
                    if (items[i].Type.SubtypeId == "Cobalt") maxAmount = (VRage.MyFixedPoint)220.00;
                    if (items[i].Type.SubtypeId == "Silicon") maxAmount = (VRage.MyFixedPoint)15.00;
                    if (items[i].Type.SubtypeId == "Magnesium") maxAmount = (VRage.MyFixedPoint)10;
                    if (items[i].Type.SubtypeId == "Silver") maxAmount = (VRage.MyFixedPoint)20.00;
                    if (items[i].Type.SubtypeId == "Gold") maxAmount = (VRage.MyFixedPoint)20.00;
                    if (items[i].Type.SubtypeId == "Platinum") maxAmount = (VRage.MyFixedPoint)5;
                    if (items[i].Type.SubtypeId == "Uranium") maxAmount = (VRage.MyFixedPoint)5;

                    maxAmount *= 5; // allow this times as much as needed 

                    if (items[i].Amount > maxAmount)
                    {
                        //Echo("Moving " + items[i].Type.SubtypeId);
                        inv.TransferItemTo(ingotDump.GetInventory(0), i, null, true, items[i].Amount - maxAmount);
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
            GridTerminalSystem.GetBlocks(invBlocks);

            List<MyInventoryItem> items;
            MyInventoryItem item;
            IMyInventory inv;
            IMyTerminalBlock blk;
            string itemBlu;
            double amt;
            for (int i = 0; i < invBlocks.Count; i++)
            {
                blk = invBlocks[i];

                if (blk.IsSameConstructAs(this.Me) && blk.HasInventory)
                {
                    if (blk.CustomName.Contains("[Ingot Dump]") && blk.IsSameConstructAs(this.Me) && !blk.GetInventory(0).IsFull)
                    {
                        ingotDump = blk as IMyCargoContainer;
                    }
                    for (int iCnt = 0; iCnt < blk.InventoryCount; iCnt++)
                    {
                        //Echo("Processing " + blk.CustomName + " inv " + iCnt);
                        inv = blk.GetInventory(iCnt);
                        if (inv != null)
                        {
                            items = new List<MyInventoryItem>();
                            inv.GetItems(items);
                            //Echo("Found " + items.Count);
                            for (int k = 0; k < items.Count; k++)
                            {
                                item = items[k];
                                //if (item.Type.TypeId != "MyObjectBuilder_Ingot")
                                if (item.Type.TypeId == "MyObjectBuilder_PhysicalGunObject" ||
                                    item.Type.TypeId == "MyObjectBuilder_AmmoMagazine" ||
                                    item.Type.TypeId == "MyObjectBuilder_Component" ||
                                    item.Type.TypeId == "MyObjectBuilder_GasContainerObject" ||
                                    item.Type.TypeId == "MyObjectBuilder_OxygenContainerObject")
                                {
                                    if (TypeToBlueprint.TryGetValue(item.Type.TypeId + "/" + item.Type.SubtypeId, out itemBlu))
                                    {
                                        amt = 0;
                                        if (invAmounts.TryGetValue(itemBlu, out amt))
                                        {
                                            invAmounts[itemBlu] = amt + (double)item.Amount;
                                        }
                                        else
                                        {
                                            invAmounts.Add(itemBlu, (double)item.Amount);
                                        }
                                    }
                                    else
                                    {
                                        //Echo("Err: " + item.Type.TypeId + "/" + item.Type.SubtypeId);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //Echo("Done Getting Items From Grid");
            //foreach(KeyValuePair<string, double> p in invAmounts)
            //{
            //    Echo("Inv " + p.Key + " - " + p.Value);
            //}
        }

        public List<AutoAsselber> GetQueuedAmounts()
        {
            //Get Production Blocks
            List<IMyTerminalBlock> prod = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyProductionBlock>(prod);

            Dictionary<string, AutoQueueItem> thisQueue;
            string blu;
            double piAmount;
            List<AutoAsselber> autoAsselbers = new List<AutoAsselber>();
            List<MyProductionItem> pQ;

            MyProductionItem pI;
            //Go through productions and add auto assemblers to a list with their queue, also create a total queued.
            for (int i = 0; i < prod.Count; i++)
            {
                IMyProductionBlock p = (IMyProductionBlock)prod[i];
                if (p.IsSameConstructAs(this.Me) && p.Enabled)
                {
                    //if its auto, is on the same construct / grid, and is enabled
                    if (p.CustomName.Contains("[Auto]") && p.IsSameConstructAs(this.Me) && p.Enabled)
                    {
                        //p.ClearQueue();
                        //Get Production Queue
                        pQ = new List<MyProductionItem>();
                        thisQueue = new Dictionary<string, AutoQueueItem>();
                        p.GetQueue(pQ);
                        for (int j = 0; j < pQ.Count; j++)
                        {
                            pI = pQ[j];
                            //Convert to amount and blueprint name
                            blu = pI.BlueprintId.ToString();
                            piAmount = (double)pI.Amount;

                            CreateOrAudition(thisQueue, blu, new AutoQueueItem { Amount = (double)pI.Amount, QueueIndex = j });
                        }
                        //Keep a list of all the auto assemblers
                        autoAsselbers.Add(new AutoAsselber(p, thisQueue));
                    }
                }
            }
            return autoAsselbers;
        }

        private void CreateOrAudition(Dictionary<string, AutoQueueItem> pairs, string key, AutoQueueItem value)
        {
            AutoQueueItem outval;
            if (pairs.TryGetValue(key, out outval))
                outval.Amount += value.Amount;
            else
                pairs.Add(key, value);
        }
    }
}