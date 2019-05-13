using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        /// <summary>
        /// This Script automatically queues stuff to assemblers.   Change the 'AddToRequiredItemsList' lines to control numbers.
        /// Put [Auto] in any assembler you want autmatically controled.  It will also start pulling from other assemblers when
        /// it has finished its queues.  If you create a cargo container with '[Ingot Dump]' in the name the script will control 
        /// ingots in the assemblers and move extra into that container.
        /// 
        /// ***** If you build more assemblers or rename them you MUST recompile the script.  This is to be a little kinder on servers.
        /// ***** I may change this to do the same for cargo containers unless i find a better way of triggering an update using events/flags (or something).
        /// </summary>
        MyIni _ini = new MyIni();

        Dictionary<string, int> largeList;
        Dictionary<string, double> invAmounts;
        Dictionary<string, string> TypeToBlueprint;
        IMyCargoContainer ingotDump = null;
        double defaultQueueAmount = 20f;
        int clogCheckNumber = 10;
        int clogCheckCurrentCount = 0;
        private const string PullingFromAssemblers = "PullingFromAssemblers";
        private const string PullingFromList = "PullingFromList";
        IMyTextPanel outputPanel = null;
        string invLabel = "ItemLimits";
        string setupLabel = "AASetup";

        List<AssemblerWithQueue> otherAssemblers;
        List<AssemblerWithQueue> autoAssemblers;


        public struct AssemblerWithQueue
        {
            public AssemblerWithQueue(IMyProductionBlock myProduction, Dictionary<string, AutoQueueItem> queue)
            {
                Assembler = myProduction;
                Queue = queue;
            }

            public IMyProductionBlock Assembler { get; set; }
            public Dictionary<string, AutoQueueItem> Queue { get; set; }

            //Updates the queue for this list
            public void UpdateQueue(StringBuilder status = null)
            {
                Queue.Clear();

                //Get the assemblers queue
                List<MyProductionItem> pQ = new List<MyProductionItem>();
                MyProductionItem pI;
                Assembler.GetQueue(pQ);
                if (status != null)
                {
                    foreach (var p in pQ)
                        status.Append(p.BlueprintId.SubtypeName + ":" + p.Amount + Environment.NewLine);
                }

                //Go through assembly queue
                string blu;
                double piAmount;
                for (int j = 0; j < pQ.Count; j++)
                {
                    pI = pQ[j];
                    //Convert to amount and blueprint name
                    blu = pI.BlueprintId.ToString();
                    piAmount = (double)pI.Amount;

                    //Add Item to queue amounts
                    AutoQueueItem outval;
                    if (Queue.TryGetValue(blu, out outval))
                    {
                        outval.Amount += outval.Amount;
                    }
                    else
                    {
                        outval = new AutoQueueItem { Amount = (double)pI.Amount, QueueIndex = j };
                        Queue.Add(blu, outval);
                    }
                }
            }
        }

        public struct AutoQueueItem
        {
            public double Amount { get; set; }
            public int QueueIndex { get; set; }
        }

        public Program()
        {
            //Update every 100 ticks
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            //Runtime.UpdateFrequency = UpdateFrequency.Once;
            Setup();
            
        }

        public void Setup(StringBuilder status = null)
        {
            //Type to blueprint listings
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
            
            Setup2(true);
        }

        public void Setup2(bool force = false, StringBuilder status = null)
        {
            string iniString = Me.CustomData;
            //If we have no custom data lets setup the string
            MyIniParseResult parseResult;
            _ini.TryParse(iniString, out parseResult);
            if (Me.CustomData == null || Me.CustomData == "" || !parseResult.Success)
            {
                status?.AppendLine("No Ini - Creating");
                _ini = new MyIni();
                _ini.Clear();
                _ini.Set(setupLabel, "OutputLCD", "AutoAssembler LCD");
                _ini.Set(invLabel, "ConstructionComponent", 20000);
                _ini.Set(invLabel, "MetalGrid", 5000);
                _ini.Set(invLabel, "InteriorPlate", 4000);
                _ini.Set(invLabel, "SteelPlate", 30000);
                _ini.Set(invLabel, "GirderComponent", 500);
                _ini.Set(invLabel, "SmallTube", 4000);
                _ini.Set(invLabel, "LargeTube", 2000);
                _ini.Set(invLabel, "MotorComponent", 2000);
                _ini.Set(invLabel, "Display", 500);
                _ini.Set(invLabel, "BulletproofGlass", 1000);
                _ini.Set(invLabel, "ComputerComponent", 1000);
                _ini.Set(invLabel, "ReactorComponent", 1000);
                _ini.Set(invLabel, "ThrustComponent", 5000);
                _ini.Set(invLabel, "GravityGeneratorComponent", 100);
                _ini.Set(invLabel, "MedicalComponent", 50);
                _ini.Set(invLabel, "RadioCommunicationComponent", 200);
                _ini.Set(invLabel, "DetectorComponent", 1000);
                _ini.Set(invLabel, "ExplosivesComponent", 200);
                _ini.Set(invLabel, "SolarCell", 200);
                _ini.Set(invLabel, "PowerCell", 500);
                _ini.Set(invLabel, "Superconductor", 1000);
                _ini.Set(invLabel, "ShieldComponentBP", 500);
                _ini.Set(invLabel, "NATO_5p56x45mmMagazine", 100);    //Personal Ammo
                _ini.Set(invLabel, "NATO_25x184mmMagazine", 500);     //Ship Ammp
                _ini.Set(invLabel, "Missile200mm", 200);
                _ini.Set(invLabel, "AngleGrinder4", 5);
                _ini.Set(invLabel, "HandDrill4", 5);
                _ini.Set(invLabel, "Welder4", 5);
                _ini.Set(invLabel, "UltimateAutomaticRifle", 5);
                _ini.Set(invLabel, "HydrogenBottle", 5);
                _ini.Set(invLabel, "OxygenBottle", 5);
                Me.CustomData = _ini.ToString();
            }

            //Get the assembler info
            GetAssemblers();

            status?.Append("Finding LCD Panel - ");
            List<MyIniKey> keys = new List<MyIniKey>();
            var lcdName = _ini.Get(setupLabel, "OutputLCD").ToString(null);
            status?.AppendLine(lcdName ?? "No LCD Name Found");
            outputPanel = GridTerminalSystem.GetBlockWithName(lcdName) as IMyTextPanel;
            if (outputPanel == null)
            {
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.GetBlocks(blocks);
                for (int i = 0; i < blocks.Count; i++)
                {
                    IMyTextPanel outPanel = blocks[i] as IMyTextPanel;
                    if (outPanel != null && outPanel.CustomName.ToLower() == lcdName.ToLower())
                    {
                        outputPanel = outPanel;
                        break;
                    }
                }
            }
            if (outputPanel != null)
            {
                status?.AppendLine("Found LCD");
                outputPanel.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                outputPanel.FontSize = 1.0f;
            }
            else
            {
                status?.AppendLine("Could not find LCD");
            }

            keys = new List<MyIniKey>();
            _ini.GetKeys(invLabel, keys);
            status?.AppendLine($"Got {keys.Count} invListings");
            largeList = new Dictionary<string, int>();
            int amount;
            foreach (var k in keys)
            {
                amount = _ini.Get(invLabel, k.Name).ToInt32(0);
                if (amount > 0) largeList.Add($"MyObjectBuilder_BlueprintDefinition/{k.Name}", amount);
                status?.AppendLine($"{k.Name} - {amount}");

            }
        }


        public void Save()
        {
        }


        public void Main(string argument, UpdateType updateSource)
        {
            StringBuilder status = new StringBuilder();
            //Setup(status);
            //Update the queues
            //status.AppendLine("Getting Assembler Queues");
            GetQueuedAmountsAllAssemblers();

            //Get the inventory
            //status.AppendLine("Getting Inventory Numbers");
            FindItems();
            //status.AppendLine("Refresh #" + clogCheckCurrentCount);
            //go every half interval
            if (clogCheckCurrentCount == clogCheckNumber / 2)
            {
                Setup2();
            }

            if (ingotDump == null) status.AppendLine("Cannot find Ingot Dump - Create Container with [Ingot Dump] in its name.");
            //go every full interval
            if (clogCheckCurrentCount >= clogCheckNumber)
            {
                if (ingotDump != null)
                {
                    
                    IMyProductionBlock p;
                    //Go through assemblers and clear any cloged
                    for (int i = 0; i < autoAssemblers.Count; i++)
                    {
                        p = autoAssemblers[i].Assembler;
                        ChkAssemblerClog(p);
                    }
                    
                }
                clogCheckCurrentCount = 0;
            }
            else
            {
                clogCheckCurrentCount++;
            }

            //check clog count, if its greater than then lets check clogs, ie. only check every x counter
            

            //Process the queues / requirements
            //Work out what we need
            if (autoAssemblers.Count > 0)
            {
                //status.AppendLine("Processing Queues/Requirements");

                status.AppendLine("Detected " + autoAssemblers.Count + " Auto Assemblers");

                //Process Queue form list, if we queue nothing lets process items from the other assemblers!
                if (!AutoQueueFromList(status, autoAssemblers))
                {
                    AutoQueueFromOtherAssemblers(status, autoAssemblers, otherAssemblers);
                }
            }
            else
            {
                status.AppendLine("Could not find assembler.  Please put [Auto] in an assembler name and recompile.");
            }
            if (outputPanel == null)
                Echo(status.ToString());
            else
                outputPanel.WriteText(status);
        }

        private bool AutoQueueFromList(StringBuilder status, List<AssemblerWithQueue> autoAssemblers)
        {
            bool hasQueuedItems = false;
            MyDefinitionId blueprint;

            double amountStored = 0;
            double amountQueued = 0;
            AutoQueueItem amountThisQueue;
            bool handtool = false;
            long counter = 0;

            bool addedConstructing = false;
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

                if (amountStored < req.Value)
                {
                    hasQueuedItems = true;
                    //Going to try to queue things
                    //Process all assemblers
                    foreach (var autoAsselber in autoAssemblers)
                    {
                        //If we are not pulling from other assemblers or we were but have run out lets check it form the list
                        if (!(autoAsselber.Assembler.CustomData == PullingFromAssemblers && autoAsselber.Queue.Count > 0))
                        {
                            autoAsselber.Assembler.CustomData = PullingFromList;
                            //If this item is not queued in this assembler lets process it and add
                            if (!autoAsselber.Queue.TryGetValue(req.Key, out amountThisQueue))
                            {
                                //Get the blueprint
                                blueprint = MyDefinitionId.Parse(req.Key);
                                if (blueprint != null)
                                {
                                    try
                                    {
                                        //Check we can use the blueprint
                                        if (autoAsselber.Assembler.CanUseBlueprint(blueprint))
                                        {
                                            amountQueued += amountThisQueue.Amount; //Add the current amount queued to the total amount queued
                                            counter = 0;
                                            //If its not a hand tool queue like normal
                                            if (handtool == false)
                                            {
                                                //Add the default amount to the queue
                                                autoAsselber.Assembler.AddQueueItem(blueprint, defaultQueueAmount);
                                                //Add the amount we queued to the counter
                                                counter += Convert.ToInt32(defaultQueueAmount);
                                            }
                                            else
                                            {
                                                //if its a hand tool queue one and only queue upto the correct amount
                                                if (counter > req.Value || amountStored > req.Value) break;
                                                autoAsselber.Assembler.AddQueueItem(blueprint, 1f);
                                                counter++;
                                            }
                                        }
                                    }
                                    catch { }
                                    //Add the amount we just queued to the total queued for this item
                                    amountQueued += counter;
                                }
                            }
                            else
                            {
                                //If we already a queued amount lets just add it total queued amount
                                amountQueued += amountThisQueue.Amount;
                            }
                        }
                    }
                    //Sort out the display of status
                    string displayKey;
                    //Add Info to statu

                    int outputLen;

                    if (addedConstructing == false)
                    {
                        status.AppendLine("Making the Following:");
                        addedConstructing = true;
                    }
                    if (outputPanel == null) outputLen = 15;
                    else outputLen = 100;

                    status.Append("(");
                    //Amount Stored
                    if (amountStored >= 1000)
                        status.Append((Math.Round(amountStored / 1000, 1).ToString() + "k").PadLeft(4, ' '));
                    else
                        status.Append(amountStored.ToString().PadLeft(4, ' '));
                    //Aount Queued
                    //status.Append("(" + amountQueued + ")");

                    status.Append("/");
                    //Total Required
                    if (req.Value >= 1000)
                        status.Append((Math.Round(Convert.ToDouble(req.Value) / 1000, 1).ToString() + "k").PadLeft(4, ' '));
                    else
                        status.Append(req.Value.ToString().PadLeft(4, ' '));
                    //Name
                    status.Append(") - ");
                    displayKey = req.Key.Substring(req.Key.IndexOf('/') + 1);
                    if (displayKey.Length > outputLen)
                        status.Append(displayKey.Substring(0, 10));
                    else
                        status.Append(displayKey);
                    status.AppendLine();
                }
            }
            return hasQueuedItems;
        }

        private void AutoQueueFromOtherAssemblers(StringBuilder status, List<AssemblerWithQueue> autoAssemblers, List<AssemblerWithQueue> otherAssemblers)
        {
            status.AppendLine("Nothing Queued - Checking Other Assemblers");
            MyDefinitionId blueprint;
            bool gotToEndOfAutos = false;
            int autoAssNumber = 0;
            double queueAmount = 0;
            double addedOfItem = 0;
            AssemblerWithQueue autoA;
            var emptyAutoA = autoAssemblers.Where(d => d.Queue.Count == 0).ToList();
            status.AppendLine("Found " + emptyAutoA.Count + " Auto Assemblers to use.");
            if (emptyAutoA.Count > 0)
            {
                foreach (var o in otherAssemblers)
                {
                    foreach (var item in o.Queue)
                    {
                        addedOfItem = 0;
                        var queueItem = item.Value;
                        blueprint = MyDefinitionId.Parse(item.Key);
                        while (queueItem.Amount > 0 && gotToEndOfAutos == false)
                        {
                            if (queueItem.Amount > defaultQueueAmount)
                            {
                                queueAmount = defaultQueueAmount;
                                queueItem.Amount = queueItem.Amount - defaultQueueAmount;
                            }
                            else
                            {
                                queueAmount = item.Value.Amount;
                                queueItem.Amount = 0;
                            }
                            if (autoAssNumber < emptyAutoA.Count)
                            {
                                autoA = emptyAutoA[autoAssNumber];
                                if (autoA.Assembler.CustomData == null && autoA.Assembler.CustomData == "")
                                {
                                    autoA.Assembler.CustomData = PullingFromAssemblers;
                                }
                                //remove form this assembler and add to auto
                                o.Assembler.RemoveQueueItem(queueItem.QueueIndex, queueAmount);
                                autoA.Assembler.AddQueueItem(blueprint, queueAmount);
                                addedOfItem += queueAmount;
                                //Next assembler
                                autoAssNumber++;
                            }
                            else
                            {
                                gotToEndOfAutos = true;
                            }
                        }
                        if (gotToEndOfAutos) break;
                        status.Append("Added ");
                        status.Append(addedOfItem);
                        status.Append(" of ");
                        status.Append(item.Key);
                    }
                    if (gotToEndOfAutos) break;
                }
            }
        }

        private void ChkAssemblerClog(IMyProductionBlock p)
        {
            //check we have an ingot dump
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
                        inv.TransferItemTo(ingotDump.GetInventory(0), i, null, true, items[i].Amount - maxAmount);
                    }
                }
            }
        }


        //Finds Items of types we care about
        public void FindItems()
        {
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

            IMyCargoContainer tstCargo;
            IMyGasGenerator tstGen;
            IMyGasTank tstTank;

            for (int i = 0; i < invBlocks.Count; i++)
            {
                blk = invBlocks[i];
                tstCargo = blk as IMyCargoContainer;
                tstGen = blk as IMyGasGenerator;
                tstTank = blk as IMyGasTank;

                if (tstCargo != null || tstGen != null || tstTank != null)
                {
                    if (blk.IsSameConstructAs(this.Me) && blk.HasInventory)
                    {
                        if (blk.CustomName.Contains("[Ingot Dump]") && blk.IsSameConstructAs(this.Me) && !blk.GetInventory(0).IsFull)
                        {
                            ingotDump = blk as IMyCargoContainer;
                        }


                        for (int iCnt = 0; iCnt < blk.InventoryCount; iCnt++)
                        {
                            inv = blk.GetInventory(iCnt);
                            if (inv != null)
                            {
                                items = new List<MyInventoryItem>();
                                inv.GetItems(items);
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
            }
        }

        public void GetAssemblers()
        {
            //reset the lists
            List<AssemblerWithQueue> thisAutoAssemblers = new List<AssemblerWithQueue>();
            var thisOtherAssemblers = new List<AssemblerWithQueue>();

            //Get Production Blocks
            List<IMyTerminalBlock> prod = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyProductionBlock>(prod);

            //Go through productions and add auto assemblers to a list with their queue, also create a total queued.
            for (int i = 0; i < prod.Count; i++)
            {
                IMyProductionBlock p = (IMyProductionBlock)prod[i];
                //if its auto, is on the same construct / grid, and is enabled
                if (p.IsSameConstructAs(this.Me) && p.Enabled)
                {
                    if (p.CustomName.ToLower().Contains("[auto]"))
                    {
                        //Keep a list of all the auto assemblers
                        thisAutoAssemblers.Add(new AssemblerWithQueue(p, new Dictionary<string, AutoQueueItem>()));
                    }
                    else
                    {
                        //Get the other assemblers
                        thisOtherAssemblers.Add(new AssemblerWithQueue(p, new Dictionary<string, AutoQueueItem>()));
                    }
                }
            }
            if (autoAssemblers == null)
            {
                autoAssemblers = new List<AssemblerWithQueue>();
                otherAssemblers = new List<AssemblerWithQueue>();
            }
            foreach (var a in thisAutoAssemblers)
            {
                if (!autoAssemblers.Where(b => b.Assembler.EntityId == a.Assembler.EntityId).Any())
                    autoAssemblers.Add(a);
            }
            foreach (var a in thisOtherAssemblers)
            {
                if (!thisOtherAssemblers.Where(b => b.Assembler.EntityId == a.Assembler.EntityId).Any())
                    thisOtherAssemblers.Add(a);
            }
        }

        public void GetQueuedAmountsAllAssemblers()
        {
            //go through aut assemblers and update the queues
            foreach (var a in autoAssemblers) a.UpdateQueue();
            foreach (var a in otherAssemblers) a.UpdateQueue();
        }

        private void AddToRequiredItemsList(ref Dictionary<string, int> itemsList, string bluePrintName, int amount)
        {
            MyDefinitionId blueprint;
            if (MyDefinitionId.TryParse(bluePrintName, out blueprint))
            {
                if (blueprint != null)
                {
                    List<IMyTerminalBlock> prod = new List<IMyTerminalBlock>();
                    GridTerminalSystem.GetBlocksOfType<IMyProductionBlock>(prod);
                    for (int i = 0; i < prod.Count; i++)
                    {
                        IMyProductionBlock a = (IMyProductionBlock)prod[i];
                        try
                        {
                            if (blueprint != null && a.CanUseBlueprint(blueprint))
                            {
                                itemsList.Add(bluePrintName, amount);
                                break;
                            }
                        }
                        catch { }
                    }
                }
            }
        }
    }
}