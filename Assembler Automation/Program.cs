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

        Dictionary<string, string> TypeToBlueprint;

        public Program()
        {
            //Update every 10 ticks
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            //Runtime.UpdateFrequency = UpdateFrequency.Once;

            //Setup List of large blocks.
            largeList = new Dictionary<string, int>();
            largeList.Add("Construction", 1000);
            largeList.Add("MetalGrid", 200);
            largeList.Add("InteriorPlate", 1000);
            largeList.Add("SteelPlate", 1000);
            largeList.Add("Girder", 200);
            largeList.Add("SmallTube", 500);
            largeList.Add("LargeTube", 200);
            largeList.Add("Motor", 500);
            largeList.Add("Display", 100);
            largeList.Add("BulletproofGlass", 300);
            largeList.Add("Computer", 500);
            largeList.Add("Reactor", 200);
            largeList.Add("Thrust", 200);
            largeList.Add("GravityGenerator", 50);
            largeList.Add("Medical", 20);
            largeList.Add("RadioCommunication", 100);
            largeList.Add("Detector", 100);
            largeList.Add("Explosives", 50);
            largeList.Add("SolarCell", 100);
            largeList.Add("PowerCell", 200);
            largeList.Add("Superconductor", 100);

            largeList.Add("5p56x45mm", 100);        //Personal Ammo
            largeList.Add("25x184mm", 100);         //Ship Ammo
            largeList.Add("Missile200mm", 100);     //Ship Ammo

            TypeToBlueprint = new Dictionary<string, string>
            {
                { "Construction", "MyObjectBuilder_BlueprintDefinition/ConstructionComponent" },
                { "MetalGrid", "MyObjectBuilder_BlueprintDefinition/MetalGrid" },
                { "InteriorPlate", "MyObjectBuilder_BlueprintDefinition/InteriorPlate" },
                { "SteelPlate", "MyObjectBuilder_BlueprintDefinition/SteelPlate" },
                { "Girder", "MyObjectBuilder_BlueprintDefinition/GirderComponent" },
                { "SmallTube", "MyObjectBuilder_BlueprintDefinition/SmallTube" },
                { "LargeTube", "MyObjectBuilder_BlueprintDefinition/LargeTube" },
                { "Motor", "MyObjectBuilder_BlueprintDefinition/MotorComponent" },
                { "Display", "MyObjectBuilder_BlueprintDefinition/Display" },
                { "BulletproofGlass", "MyObjectBuilder_BlueprintDefinition/BulletproofGlass" },
                { "Computer", "MyObjectBuilder_BlueprintDefinition/ComputerComponent" },
                { "Reactor", "MyObjectBuilder_BlueprintDefinition/ReactorComponent" },
                { "Thrust", "MyObjectBuilder_BlueprintDefinition/ThrustComponent" },
                { "GravityGenerator", "MyObjectBuilder_BlueprintDefinition/GravityGeneratorComponent" },
                { "Medical", "MyObjectBuilder_BlueprintDefinition/MedicalComponent" },
                { "RadioCommunication", "MyObjectBuilder_BlueprintDefinition/RadioCommunicationComponent" },
                { "Detector", "MyObjectBuilder_BlueprintDefinition/DetectorComponent" },
                { "Explosives", "MyObjectBuilder_BlueprintDefinition/ExplosivesComponent" },
                { "SolarCell", "MyObjectBuilder_BlueprintDefinition/SolarCell" },
                { "PowerCell", "MyObjectBuilder_BlueprintDefinition/PowerCell" },
                { "Superconductor", "MyObjectBuilder_BlueprintDefinition/Superconductor" },
                { "25x184mm", "MyObjectBuilder_BlueprintDefinition/NATO_25x184mmMagazine" },
                { "5p56x45mm", "MyObjectBuilder_BlueprintDefinition/NATO_5p56x45mmMagazine" },
                { "Missile200mm", "MyObjectBuilder_BlueprintDefinition/Missile200mm" }
            };

            /*
            TypeToBlueprint.Add("", "MyObjectBuilder_BlueprintDefinition/AngleGrinder");
            TypeToBlueprint.Add("", "MyObjectBuilder_BlueprintDefinition/AngleGrinder2");
            TypeToBlueprint.Add("", "MyObjectBuilder_BlueprintDefinition/AngleGrinder3");
            TypeToBlueprint.Add("", "MyObjectBuilder_BlueprintDefinition/AngleGrinder4");
            TypeToBlueprint.Add("", "MyObjectBuilder_BlueprintDefinition/HandDrill");
            TypeToBlueprint.Add("", "MyObjectBuilder_BlueprintDefinition/HandDrill2");
            TypeToBlueprint.Add("", "MyObjectBuilder_BlueprintDefinition/HandDrill3");
            TypeToBlueprint.Add("", "MyObjectBuilder_BlueprintDefinition/HandDrill4");
            TypeToBlueprint.Add("", "MyObjectBuilder_BlueprintDefinition/Welder");
            TypeToBlueprint.Add("", "MyObjectBuilder_BlueprintDefinition/Welder2");
            TypeToBlueprint.Add("", "MyObjectBuilder_BlueprintDefinition/Welder3");
            TypeToBlueprint.Add("", "MyObjectBuilder_BlueprintDefinition/Welder4");
            TypeToBlueprint.Add("", "MyObjectBuilder_BlueprintDefinition/AutomaticRifle");
            TypeToBlueprint.Add("", "MyObjectBuilder_BlueprintDefinition/PreciseAutomaticRifle");
            TypeToBlueprint.Add("", "MyObjectBuilder_BlueprintDefinition/RapidFireAutomaticRifle");
            TypeToBlueprint.Add("", "MyObjectBuilder_BlueprintDefinition/UltimateAutomaticRifle");
            TypeToBlueprint.Add("", "MyObjectBuilder_BlueprintDefinition/HydrogenBottle");
            TypeToBlueprint.Add("", "MyObjectBuilder_BlueprintDefinition/OxygenBottle");
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

            bool found = false;
            StringBuilder status; 
            MyDefinitionId blueprint;
            string blueprintName;

            int i;
            for (i = 0; i < prod.Count; i++)
            {
                IMyProductionBlock p = (IMyProductionBlock)prod[i];
                //Echo("Processing " + p.CustomName);
                if (p.CustomName.Contains("[Primary]"))
                {
                    Echo("Processing Inventory");
                    //Get Queue
                    List<MyProductionItem> queue = new List<MyProductionItem>();
                    p.GetQueue(queue);

                    //Work out what we need
                    foreach(KeyValuePair<string, int> req in largeList)
                    {
                        double amt = 0;
                        invAmounts.TryGetValue(req.Key, out amt);
                        status = new StringBuilder(req.Key);
                        status.Append(" - ");
                        status.Append(amt);
                        status.Append("/");
                        status.Append(req.Value);

                        //Test if we need those!
                        if (amt < req.Value)
                        {
                            //Test Queue for this item, if we dont have any in the queue the lets queue say 100?
                            blueprintName = TypeToBlueprint[req.Key];
                            if (!queue.Where(d => d.BlueprintId.ToString() == blueprintName).Any())
                            {
                                status.Append(" - Adding");
                                //Echo(req.Key);
                                blueprint = MyDefinitionId.Parse(blueprintName);
                                p.AddQueueItem(blueprint, 100f);
                            }
                            else
                            {
                                status.Append(" - In List");
                            }
                        }
                        else
                        {
                            status.Append(" - Full");
                        }
                        Echo(status.ToString());
                    }
                    found = true;
                    break;
                }
            }

            if (found == false)
            {
                Echo("Could not find assembler.  Please put [Primary] in an assembler name.");
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