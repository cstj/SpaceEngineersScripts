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
        Dictionary<string, double> queuedAmounts;
        Dictionary<string, string> TypeToBlueprint;
        IMyCargoContainer ingotDump = null;

        Double AddAmount =50f;

        public Program()
        {
            //Update every 10 ticks
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            //Runtime.UpdateFrequency = UpdateFrequency.Once;

            //Setup List of large blocks.
            largeList = new Dictionary<string, int>();
            largeList.Add("MyObjectBuilder_BlueprintDefinition/ConstructionComponent", 4000);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/MetalGrid", 200);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/InteriorPlate", 3000);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/SteelPlate", 10000);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/GirderComponent", 200);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/SmallTube", 3000);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/LargeTube", 500);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/MotorComponent", 500);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/Display", 100);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/BulletproofGlass", 300);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/ComputerComponent", 1000);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/ReactorComponent", 300);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/ThrustComponent", 1000);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/GravityGeneratorComponent", 50);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/MedicalComponent", 20);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/RadioCommunicationComponent", 100);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/DetectorComponent", 100);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/ExplosivesComponent", 50);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/SolarCell", 100);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/PowerCell", 200);
            largeList.Add("MyObjectBuilder_BlueprintDefinition/Superconductor", 100);

            largeList.Add("MyObjectBuilder_BlueprintDefinition/ShieldComponentBP", 1000);

            //Ammo
            largeList.Add("MyObjectBuilder_BlueprintDefinition/NATO_5p56x45mmMagazine", 100);        //Personal Ammo
            largeList.Add("MyObjectBuilder_BlueprintDefinition/NATO_25x184mmMagazine", 100);         //Ship Ammo
            largeList.Add("MyObjectBuilder_BlueprintDefinition/Missile200mm", 100);     //Ship Ammo

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
            Echo("Getting Inventory Numbers");
            FindItems();
            Echo("Getting Queues");
            GetQueuedAmounts();

            //Echo("Getting List of Production Blocks");
            List<IMyTerminalBlock> prod = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyProductionBlock>(prod);

            StringBuilder status; 
            MyDefinitionId blueprint;

            int i;
            double amt = 0;
            double amtQueued = 0;
            //Find automated assemblers
            List<KeyValuePair<IMyProductionBlock, double>> autoAssemblers = new List<KeyValuePair<IMyProductionBlock, double>>();
            //List<MyProductionItem> queueItems;
            Echo("Cleaning Assemblers");
            for (i = 0; i < prod.Count; i++)
            {
                IMyProductionBlock p = (IMyProductionBlock)prod[i];
                //Echo("Processing " + p.CustomName);
                //Is producing, on the same construct and is on
                if (p.CustomName.Contains("[Auto]") && p.IsSameConstructAs(this.Me) && p.Enabled)
                {
                    autoAssemblers.Add(new KeyValuePair<IMyProductionBlock, double>(p, 0));
                    ChkAssemblerClog(p);
                }
            }

            Echo("Processing Queues/Requirements");

            string displayKey;
            //Work out what we need
            if (autoAssemblers.Count > 0)
            {
                double addToQueue = Math.Ceiling(AddAmount / autoAssemblers.Count);
                Echo("Detected " + autoAssemblers.Count + " Auto Assemblers");
                Echo("Will queue " + addToQueue + " at a time");

                double chkAmount = 10;
                bool handtool = false;
                long counter = 0;
                foreach (KeyValuePair<string, int> req in largeList)
                {
                    //If we're working with hand tools, queue one and queue on 0
                    if (req.Key.StartsWith("MyObjectBuilder_BlueprintDefinition/AngleGrinder") ||
                        req.Key.StartsWith("MyObjectBuilder_BlueprintDefinition/HandDrill") ||
                        req.Key.StartsWith("MyObjectBuilder_BlueprintDefinition/Welder") ||
                        req.Key.EndsWith("AutomaticRifle") ||
                        req.Key.EndsWith("Bottle"))
                    {
                        handtool = true;
                        chkAmount = 0;
                    }
                    else
                    {
                        handtool = false;
                        chkAmount = autoAssemblers.Count * 3;
                    }

                    //Get Current Inv
                    //Echo("Getting Current inv of " + req.Key);
                    amt = 0;
                    invAmounts.TryGetValue(req.Key, out amt);
                    //Get Queued Amount
                    //Echo("Getting Queued Amt of " + req.Key);
                    amtQueued = 0;
                    queuedAmounts.TryGetValue(req.Key, out amtQueued);
                    //Echo(amt + " - " + amtQueued);

                    displayKey = req.Key.Substring(req.Key.IndexOf('/') + 1);
                    if (displayKey.Length > 10)
                        status = new StringBuilder(displayKey.Substring(0, 10));
                    else
                        status = new StringBuilder(displayKey);
                    status.Append(" ");
                    status.Append(amt);
                    status.Append("(" + amtQueued + ")");
                    status.Append("/");
                    status.Append(req.Value);

                    //Test if we need those!
                    if (amt > req.Value)
                        status.Append(" - Full");
                    else if (amtQueued > chkAmount)
                        status.Append(" - Queued");
                    else
                    {
                        status.Append(" - Add");
                        blueprint = MyDefinitionId.Parse(req.Key);
                        counter = 0;
                        foreach (var p in autoAssemblers)
                        {
                            if (handtool == false)
                                p.Key.AddQueueItem(blueprint, addToQueue);
                            else
                            {
                                Echo("Err - Cant Make " + req.Key);
                            }
                        }
                    }
                    Echo(status.ToString());
                }
            }
            else
            {
                Echo("Could not find assembler.  Please put [Auto] in an assembler name.");
            }
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

        public void GetQueuedAmounts()
        {
            List<IMyTerminalBlock> prod = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyProductionBlock>(prod);
            queuedAmounts = new Dictionary<string, double>();
            string blu;
            int i;
            List<MyProductionItem> pQ;
            for (i = 0; i < prod.Count; i++)
            {
                //Echo("Getting Assembler");
                IMyProductionBlock p = (IMyProductionBlock)prod[i];
                //Echo(p.CustomName);
                if (p.IsSameConstructAs(this.Me) && p.Enabled)
                {
                    pQ = new List<MyProductionItem>();
                    p.GetQueue(pQ);
                    foreach (var pI in pQ)
                    {
                        blu = pI.BlueprintId.ToString();
                        if (queuedAmounts.ContainsKey(blu))
                            queuedAmounts[blu] += (double)pI.Amount;
                        else
                            queuedAmounts.Add(blu, (double)pI.Amount);

                    }
                }
            }
            /*
            foreach(var p in queuedAmounts)
            {
                Echo("Queued - " + p.Key + " : " + p.Value);
            }
            */
        }
    }
}