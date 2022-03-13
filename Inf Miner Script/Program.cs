using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        
        public float SpeedDescend1 = .1f;
        public float SpeedDescend2 = .015f;
        public float SpeedAscend = 1f;

        public float DescendLength2Min = .35f;
        public float DescendLength2Max = .6f;
        public float DescendLengthBottom = 3.95f;
        public float DescendLengthTop = 0;

        public double LimitStop = 90;
        public double LimitContinue = 50;


        Dictionary<string, string> TypeToBlueprint;
        Dictionary<string, int> TypeToRequired;
        double limit;
        int counter;

        public class Timer
        {
            public bool Running { get; set; }
            public DateTime startTime { get; internal set; }

            public Timer()
            {
                startTime = DateTime.Now;
                Running = false;
            }

            public void Restart()
            {
                Running = true;
                startTime = DateTime.Now;
            }

            public double TotalSeconds
            {
                get
                {
                    return Math.Round((DateTime.Now - startTime).TotalMilliseconds/1000,1);
                }
            }
        }

        public class MinerState
        {
            public static MinerState Init => new MinerState { Order = 0, State = "Init" };
            public static MinerState Init2 => new MinerState { Order = 0, State = "Init 2" };
            public static MinerState Init3 => new MinerState { Order = 0, State = "Init 3" };
            public static MinerState BuildingExtention => new MinerState { Order = 0, State = "Building Extention" };
            public static MinerState Descending => new MinerState { Order = 0, State = "Descending" };
            public static MinerState Descending2 => new MinerState { Order = 0, State = "Descending 2" };
            public static MinerState Grinding => new MinerState { Order = 0, State = "Grinding" };
            public static MinerState Retracting => new MinerState { Order = 0, State = "Retracting" };

            public static Dictionary<string, MinerState> AllStates = new Dictionary<string, MinerState>
            {
                { Init.State, Init },
                { Init2.State, Init2 },
                { Init3.State, Init3 },
                { BuildingExtention.State, BuildingExtention },
                { Descending.State, Descending },
                { Descending2.State, Descending2 },
                { Grinding.State, Grinding },
                { Retracting.State, Retracting },
            };

            public string State { get; set; }
            public int Order { get; set; }
        }

        public enum PistonMovementState
        {
            Moving,
            Static,
            Unk,
        }
        
        public enum PistonDirection
        {
            Descending,
            Descending2,
            Ascending,
            NA
        }

        public class PistonAndState
        {
            public IMyPistonBase Piston { get; set; }
            public float LastPosition { get; set; }
            public PistonMovementState MovementState { get; set; }
        }

        public class PistonsGroup
        {
            public static float MIN_FLOAT_COMP = 0.000000001f;

            public PistonsGroup()
            {
                Pistons = new List<PistonAndState>();
            }

            public List<PistonAndState> Pistons { get; set; }

            public float CurrentLength
            {
                get
                {
                    float totalLength = 0;
                    foreach (var p in Pistons) totalLength += p.Piston.CurrentPosition;
                    return totalLength;
                }
            }

            public float LastLength
            {
                get
                {
                    float totalLength = 0;
                    foreach (var p in Pistons) totalLength += p.LastPosition;
                    return totalLength;
                }
            }


            public PistonDirection MovementDirection
            {
                get
                {
                    if (Math.Abs(CurrentLength - LastLength) < MIN_FLOAT_COMP) return PistonDirection.NA;
                    else if (CurrentLength - LastLength > 0) return PistonDirection.Descending;
                    else return PistonDirection.Ascending;
                }
            }

            public PistonMovementState MovementState
            {
                get
                {
                    PistonMovementState tmpState = PistonMovementState.Static;
                    foreach (var p in Pistons)
                    {
                        if (p.MovementState == PistonMovementState.Moving)
                        {
                            tmpState = PistonMovementState.Moving;
                            break;
                        }
                        else if (p.MovementState == PistonMovementState.Unk)
                        {
                            tmpState = PistonMovementState.Unk;
                            break;
                        }
                    }
                    return tmpState;
                }
            }

            public string UpdatePistons(MinerState curState)
            {
                StringBuilder sb = new StringBuilder();
                PistonAndState p;
                for (int i = 0; i < Pistons.Count; i++)
                {
                    p = Pistons[i];
                    // less than x?
                    if (Math.Abs(p.LastPosition - p.Piston.CurrentPosition) > MIN_FLOAT_COMP)
                    {
                        p.MovementState = PistonMovementState.Moving;
                        p.LastPosition = p.Piston.CurrentPosition;
                    }
                    //If we're not in init, we must be stoped as there is almost no change
                    else if (curState.State != MinerState.Init.State)
                    {
                        p.MovementState = PistonMovementState.Static;
                        p.LastPosition = p.Piston.CurrentPosition;
                    }
                }
                if (Pistons[0].MovementState == PistonMovementState.Static) sb.AppendLine("Pistions: Static - " + CurrentLength);
                else sb.AppendLine("Pistions: Moving - " + CurrentLength);

                return sb.ToString();
            }

            public void Descend(float rate)
            {
                float pRate = rate / Pistons.Count;
                foreach(var p in Pistons)
                {
                    if (p.Piston.Velocity != pRate) p.Piston.Velocity = pRate;
                }
            }

            public void Ascend(float rate)
            {
                float pRate = rate / Pistons.Count;
                foreach (var p in Pistons)
                {
                    if (p.Piston.Velocity != -pRate) p.Piston.Velocity = -pRate;
                }
            }
        }

        IMyTextSurface output;

        MinerState curState = MinerState.Init;
        static string prefix = "Inf Miner ";

        string extentionPistonsString = prefix + "Extention Pistons";
        PistonsGroup extentionPistons = new PistonsGroup();

        string holderMergerString = prefix + "Holder Merge";
        IMyShipMergeBlock holderMerger;

        string drillsString = prefix + "Drills";
        List<IMyShipDrill> drills = new List<IMyShipDrill>();

        string weldersString = prefix + "Welders";
        List<IMyShipWelder> welders = new List<IMyShipWelder>();

        string grinderString = prefix + "Grinder";
        IMyShipGrinder grinder;

        string drillRotorString = prefix + "Drill Rotor";
        IMyMotorAdvancedStator drillRotor;

        Timer grindTimer = new Timer();
        double grindTimerLength = 10;

        Timer descend1Timer = new Timer();
        double descend1TimerLength = 1;

        string offLightString = prefix + "Light Off";
        IMyInteriorLight offLight;
        string onLightString = prefix + "Light On";
        IMyInteriorLight onLight;

        string projectorString = prefix + "Projector";
        IMyProjector projector;

        string invCompString = prefix + "Cargo CompStor";
        List<IMyInventory> invComp = new List<IMyInventory>();
        double invCompTotalSpace = 0;

        string invStoneString = prefix + "Cargo Stone";
        List<IMyInventory> invStone = new List<IMyInventory>();
        double invStoneTotalSpace = 0;

        string cargoIngotsString = prefix + "Cargo Instor";
        List<IMyInventory> invIngots = new List<IMyInventory>();
        double invIngotTotalSpace = 0;

        string assemblerString = prefix + "Assembler";
        IMyAssembler assembler;
        List<IMyInventory> invAssembler = new List<IMyInventory>();

        string refinaryString = prefix + "Refinery";
        List<IMyInventory> intRefinary = new List<IMyInventory>();

        string minerStateLightsString = prefix + "Light State ";
        Dictionary<string, IMyInteriorLight> minerStateLights = new Dictionary<string, IMyInteriorLight>();

        public Program()
        {
            StringBuilder sb = new StringBuilder();
            limit = LimitStop;
            sb.AppendLine("Running " + DateTime.Now);
            TypeToBlueprint = new Dictionary<string, string>
            {
                { "MyObjectBuilder_Component/Construction", "MyObjectBuilder_BlueprintDefinition/ConstructionComponent" },
                { "MyObjectBuilder_Component/InteriorPlate", "MyObjectBuilder_BlueprintDefinition/InteriorPlate" },
                { "MyObjectBuilder_Component/SteelPlate", "MyObjectBuilder_BlueprintDefinition/SteelPlate" },
                { "MyObjectBuilder_Component/SmallTube", "MyObjectBuilder_BlueprintDefinition/SmallTube" },
                { "MyObjectBuilder_Component/Motor", "MyObjectBuilder_BlueprintDefinition/MotorComponent" },
                { "MyObjectBuilder_Component/Computer", "MyObjectBuilder_BlueprintDefinition/ComputerComponent" },
            };
            TypeToRequired = new Dictionary<string, int>
            {
                { "MyObjectBuilder_BlueprintDefinition/ConstructionComponent", 31*1 + 10 },
                { "MyObjectBuilder_BlueprintDefinition/InteriorPlate", 22*1 + 8 },
                { "MyObjectBuilder_BlueprintDefinition/SteelPlate", 11*1 + 4 },
                { "MyObjectBuilder_BlueprintDefinition/SmallTube", 3*1 + 2 },
                { "MyObjectBuilder_BlueprintDefinition/MotorComponent", 16*1 + 6 },
                { "MyObjectBuilder_BlueprintDefinition/ComputerComponent", 3*1 + 2 },
            };
           
            //Get the Refrences to things
            List<IMyTerminalBlock> termBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(termBlocks);
            
            foreach(var b in termBlocks)
            {
                //Top
                if (b.CustomName == holderMergerString)
                {
                    holderMerger = b as IMyShipMergeBlock;
                    sb.AppendLine("Found Holder Merger");
                }
                else if (b.CustomName == grinderString)
                {
                    grinder = b as IMyShipGrinder;
                    sb.AppendLine("Found Grinder");
                }
                else if (b.CustomName == drillRotorString)
                {
                    drillRotor = b as IMyMotorAdvancedStator;
                    sb.AppendLine("Found Drill Rotor");
                }
                else if (b.CustomName == onLightString)
                {
                    onLight = b as IMyInteriorLight;
                    sb.AppendLine("Found On Light");
                }
                else if (b.CustomName == offLightString)
                {
                    offLight = b as IMyInteriorLight;
                    sb.AppendLine("Found Off Light");
                }
                else if (b.CustomName == projectorString)
                {
                    projector = b as IMyProjector;
                    sb.AppendLine("Found Projector");
                }
                else if (b.CustomName.StartsWith(assemblerString))
                {
                    assembler = b as IMyAssembler;
                    invAssembler.Add(b.GetInventory(1));
                    sb.AppendLine("Found Assembler");
                }
                else if (b.CustomName.StartsWith(invCompString))
                {
                    IMyInventory inv = b.GetInventory(0);
                    invComp.Add(b.GetInventory(0));
                    invCompTotalSpace += ((double)inv.MaxVolume);
                    sb.AppendLine("Found Cargo Comp");
                }
                else if (b.CustomName.StartsWith(invStoneString))
                {
                    IMyInventory inv = b.GetInventory(0);
                    invStone.Add(inv);
                    invStoneTotalSpace += ((double)inv.MaxVolume);
                    sb.AppendLine("Found Cargo Stone");
                }
                else if (b.CustomName.StartsWith(cargoIngotsString))
                {
                    IMyInventory inv = b.GetInventory(0);
                    invIngots.Add(b.GetInventory(0));
                    invIngotTotalSpace += ((double)inv.MaxVolume);
                    sb.AppendLine("Found Cargo Ingots");
                }
                else if (b.CustomName.StartsWith(refinaryString))
                {
                    IMyInventory inv = b.GetInventory(0);
                    intRefinary.Add(b.GetInventory(1));
                    sb.AppendLine("Found Refinary");
                }
                //State LIghts
                else
                {
                    foreach(var l in MinerState.AllStates.Values)
                    {
                        if (b.CustomName == minerStateLightsString + l.State)
                        {
                            sb.Append("Found State Light " + l.State);
                            minerStateLights.Add(l.State, b as IMyInteriorLight);
                        }
                    }
                }
            }
            sb.AppendLine();

            List<IMyBlockGroup> gridGroups = new List<IMyBlockGroup>();
            GridTerminalSystem.GetBlockGroups(gridGroups);
            foreach(var b in gridGroups)
            {
                if (b.Name == drillsString)
                {
                    b.GetBlocksOfType(drills) ;
                    sb.AppendLine("Found Drills");
                }
                else if (b.Name == weldersString)
                {
                    b.GetBlocksOfType(welders);
                    sb.AppendLine("Found Welders");
                }
                else if (b.Name == extentionPistonsString)
                {
                    List<IMyPistonBase> tmpPistons = new List<IMyPistonBase>();
                    b.GetBlocksOfType(tmpPistons);
                    
                    foreach(var p in tmpPistons)
                    {
                        extentionPistons.Pistons.Add(new PistonAndState { Piston = p, LastPosition = p.CurrentPosition, MovementState = PistonMovementState.Unk });
                    }

                    extentionPistons.UpdatePistons(curState);
                    sb.AppendLine("Found Extention Pistons");
                }
            }
            
            sb.AppendLine();
            //Test values
            bool envTest = true;
            if (holderMerger == null)
            {
                envTest = false;
                sb.AppendLine("Could not find \"" + holderMergerString + "\"");
            }
            if (grinder == null)
            {
                envTest = false;
                sb.AppendLine("Could not find \"" + grinderString + "\"");
            }
            if (drillRotor == null)
            {
                envTest = false;
                sb.AppendLine("Could not find \"" + drillRotorString + "\"");
            }
            if (welders.Count != 4)
            {
                envTest = false;
                sb.AppendLine("Wrong number of welders in \"" + weldersString + "\"");
            }
            if (extentionPistons.Pistons.Count != 2)
            {
                envTest = false;
                sb.AppendLine("Wrong number of welders in \"" + extentionPistonsString + "\"");
            }
            if (offLight == null || onLight == null)
            {
                envTest = false;
                sb.AppendLine("On / Off Light Missing");
            }
            if (projector == null)
            {
                envTest = false;
                sb.AppendLine("Projector not Found");
            }
            if (invStone.Count == 0)
            {
                envTest = false;
                sb.AppendLine("Cargo Stone not Found - " + invStoneString);
            }
            if (invComp.Count == 0)
            {
                envTest = false;
                sb.AppendLine("Cargo Comp not Found - " + invCompString);
            }
            if (assembler == null || invAssembler == null)
            {
                envTest = false;
                sb.AppendLine("Assembler not Found");
            }
            if (invIngots.Count == 0)
            {
                envTest = false;
                sb.AppendLine("Cargo Ingots not Found - "  + cargoIngotsString);
            }
            if (minerStateLights.Count != MinerState.AllStates.Count - 3)
            {
                envTest = false;
                sb.AppendLine("State Light Missing");
            }

            //If env is sane, set to rerun
            if (envTest)
            {
                SolveState(sb);
                sb.AppendLine("Found all required stuff, setting to rerun");
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
            }

            output = Me.GetSurface(0);
            output.FontSize = 0.7f;
            output.ContentType = ContentType.TEXT_AND_IMAGE;

            //TODO: Solve current state!
            Echo(sb.ToString());
            output.WriteText(sb.ToString());
        }

        public void Save()
        {
            
        }

        public void Main(string argument, UpdateType updateSource)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Running Main " + DateTime.Now);

            if (onLight.Enabled == true)
            {
                //Only Sort Cargo Sometimes
                if (counter > 3)
                {
                    //Echo("Sorting Cargo");
                    SortCargo();
                    counter = 0;
                    foreach (var b in intRefinary) SortCargo(b, "");
                }
                counter++;

                sb.Append("State: ").AppendLine(curState.State);
                sb.AppendLine(extentionPistons.UpdatePistons(curState));

                //Test Cargo Limits
                double curInv = 0;
                foreach (var b in invComp) curInv += ((double)b.CurrentVolume);
                var perComp = Math.Round((curInv / invCompTotalSpace) * 100, 0);
                curInv = 0;
                foreach (var b in invIngots) curInv += ((double)b.CurrentVolume);
                var perIngot = Math.Round((curInv / invIngotTotalSpace) * 100, 0);
                curInv = 0;
                foreach (var b in invStone) curInv += ((double)b.CurrentVolume);
                var perStone = Math.Round((curInv / invStoneTotalSpace) * 100, 0);

                //if a cargo is over the limit set
                if (perComp > limit
                    || (perStone > limit)
                    || (perIngot > limit))
                {
                    //Set the limit to the Continue Value
                    limit = LimitContinue;
                    sb.AppendLine("Pausing - Cargo Over Limit " + LimitStop + "%");
                    sb.AppendLine("Continue when Cargo Falls Below " + LimitContinue + "%");
                    sb.AppendLine("Ingot Cargo - " + perIngot + "%");
                    sb.AppendLine("Components Cargo  - " + perComp + "%");
                    sb.AppendLine("Stone Cargo  - " + perStone + "%");
                    //Stop all the things
                    sb.AppendLine(StateWelders(false));
                    sb.AppendLine(StateGrinder(false));
                    sb.AppendLine(StateDrills(false));
                    sb.AppendLine(StateDrillRotor(false));
                    extentionPistons.Descend(0);
                    StatePistons(false);
                }
                else
                {
                    //Start Drills and Rotors and set limit to stop limit
                    StateDrills(true);
                    StatePistons(true);
                    sb.AppendLine(StateDrillRotor(true));
                    limit = LimitStop;
                    if (curState.State == MinerState.Init.State) SetState(MinerState.Init2);
                    else if (curState.State == MinerState.Init2.State)SetState(MinerState.Init3);
                    else if (curState.State == MinerState.Init3.State) SolveState(sb);
                    else if (curState.State == MinerState.BuildingExtention.State) BuildExtention(sb);
                    else if (curState.State == MinerState.Descending.State) Descending(sb);
                    else if (curState.State == MinerState.Descending2.State) Descending2(sb);
                    else if (curState.State == MinerState.Grinding.State) Grinder(sb);
                    else if (curState.State == MinerState.Retracting.State) Retracting(sb);
                }
            }
            else
            {
                sb.AppendLine("State: Turned Off");
                sb.AppendLine(StateWelders(false));
                sb.AppendLine(StateGrinder(false));
                sb.AppendLine(StateDrills(false));
                sb.AppendLine(StateDrillRotor(false));
                extentionPistons.Descend(0);
                StatePistons(false);
                SetState(MinerState.Init);
            }
            Echo(sb.ToString());
            output.WriteText(sb.ToString());
            
        }

        private bool HaveRequiredComp(StringBuilder sb)
        {
            //List<MyInventoryItem> items = new List<MyInventoryItem>();
            Dictionary<string, int> itemsCol = new Dictionary<string, int>();
            int amount;
            string type;
            foreach (var inv in invComp) GetCompInInv(inv, itemsCol);
            foreach (var inv in invAssembler) GetCompInInv(inv, itemsCol);

            //Queue whats needed
            MyDefinitionId blueprint;
            List<MyProductionItem> queue = new List<MyProductionItem>();
            assembler.GetQueue(queue);
            Dictionary<string, int> assQueue = SortQueue(queue);
            string bptString;
            int need;
            int queuedAmt = 0;
            bool haveComp = true;
            foreach(var d in TypeToRequired)
            {
                bptString = d.Key;
                amount = 0;
                itemsCol.TryGetValue(d.Key, out amount);        //Get Amount we have
                queuedAmt = 0;
                assQueue.TryGetValue(bptString, out queuedAmt);    //Take away any more we have queued.
                need = d.Value - amount - queuedAmt;
                //Echo(d.Key + "\nR" + d.Value + "A" + amount + " QA" + queuedAmt + " N" + need);
                if (need > 0)
                {
                    haveComp = false;
                    blueprint = MyDefinitionId.Parse(bptString);
                    assembler.AddQueueItem(blueprint, Convert.ToDecimal(need));
                    sb.Append(d.Key.Substring(d.Key.IndexOf("/") + 1) + ": ");
                    sb.AppendLine("N: " + need + "(queued)");
                }
                else if (queuedAmt > 0)
                {
                    haveComp = false;
                    sb.Append(d.Key.Substring(d.Key.IndexOf("/") + 1) + ": ");
                    sb.AppendLine("N: " + queuedAmt);
                }
            }
            return haveComp;
        }

        private void GetCompInInv(IMyInventory inv, Dictionary<string, int> itemsCol)
        {
            MyInventoryItem i;
            List<MyInventoryItem> items = new List<MyInventoryItem>();
            int amount;
            string type;
            inv.GetItems(items);
            //Get amounts
            for (int k = 0; k < items.Count; k++)
            {
                i = items[k];
                amount = 0;
                type = TypeToBlueprint[i.Type.ToString()];
                itemsCol.TryGetValue(type, out amount);
                amount += (int)i.Amount;
                itemsCol[type] = amount;
            }
        }

        private Dictionary<string, int> SortQueue(List<MyProductionItem> queue)
        {
            int amt;
            Dictionary<string, int> outList = new Dictionary<string, int>();
            string bptid;
            foreach(var q in queue)
            {
                amt = 0;
                bptid = q.BlueprintId.ToString();
                outList.TryGetValue(bptid, out amt);
                amt += ((int)q.Amount);
                outList[bptid] = amt;
            }
            return outList;
        }

        int queueCounter = 0;
        int sortNumber = 0;
        private void SortCargo() 
        {
            if (sortNumber == 0) foreach (var b in invComp) SortCargo(b, "MyObjectBuilder_Component");
            if (sortNumber == 1) foreach (var b in invIngots) SortCargo(b, "MyObjectBuilder_Ingot");
            if (sortNumber == 2) foreach (var b in invStone) SortCargo(b, "MyObjectBuilder_Ore");
            if (sortNumber == 3) foreach (var b in invAssembler) SortCargo(b, "");
            if (sortNumber == 3) foreach (var b in intRefinary) SortCargo(b, "");

            if (queueCounter > 10)
            {
                //HaveRequiredComp(new StringBuilder());
                queueCounter = 0;
            }
            queueCounter++;
            sortNumber++;
            if (sortNumber >= 3)
            {
                sortNumber = 0;
            }
        }

        private void SortCargo(IMyInventory inv, string notMove)
        {
            List<MyInventoryItem> items = new List<MyInventoryItem>();
            MyInventoryItem item;
            inv.GetItems(items);
            for (int k = 0; k < items.Count; k++)
            {
                item = items[k];
                SortCargoTestItem(inv, invComp, item, "MyObjectBuilder_Component", notMove);
                SortCargoTestItem(inv, invIngots, item, "MyObjectBuilder_Ingot", notMove);
                SortCargoTestItem(inv, invStone, item, "MyObjectBuilder_Ore", notMove);
            }
        }

        public void SortCargoTestItem(IMyInventory fromInv, List<IMyInventory> toInv, MyInventoryItem item, string testType, string notMove)
        {
            MyFixedPoint amtLeft;
            if (item.Type.TypeId == testType && item.Type.TypeId != notMove)
            {
                amtLeft = item.Amount;
                foreach (var b in toInv)
                {
                    if (!b.IsFull) fromInv.TransferItemTo(b, item);
                    if (amtLeft == 0) break;
                }
            }
        }

        private void StateProjector(bool state)
        {
            if (projector.Enabled ==  !state) projector.Enabled = state;
        }

        private string StateDrills(bool state)
        {
            if (drills[0].Enabled == !state) foreach (var w in drills) w.Enabled = state;
            if (state) return "Drills: Enabled";
            else return "Drills: Disabled";
        }

        private string StateDrillRotor(bool state)
        {
            if (drillRotor.Enabled == !state) drillRotor.Enabled = state;
            if (drillRotor.TargetVelocityRPM != 2) drillRotor.TargetVelocityRPM = 2;
            if (state) return "Drill Rotor: Enabled";
            else return "Drill Rotor: Disabled";
        }

        private string StateWelders(bool state)
        {
            if (welders[0].Enabled == !state) foreach (var w in welders) w.Enabled = state;
            if (state) return "Welders: Enabled";
            else return "Welders: Disabled";
        }

        private string StateMergeHolder(bool state)
        {
            if (holderMerger.Enabled == !state) holderMerger.Enabled = state;
            if (state) return "Merge Holder: Enabled";
            else return "Merge Holder: Disabled";
        }

        private string StateGrinder(bool state)
        {
            if (grinder.Enabled == !state) grinder.Enabled = state;
            if (state) return "Grinder: Enabled";
            else return "Grinder: Disabled";
        }

        private void StatePistons(bool state)
        {
            if (extentionPistons.Pistons[0].Piston.Enabled == !state) foreach (var w in extentionPistons.Pistons) w.Piston.Enabled = state;
        }

        float GetMyTerminalBlockHealth(IMyTerminalBlock block)
        {
            IMySlimBlock slimblock = block.CubeGrid.GetCubeBlock(block.Position);
            float MaxIntegrity = slimblock.MaxIntegrity;
            float BuildIntegrity = slimblock.BuildIntegrity;
            float CurrentDamage = slimblock.CurrentDamage;
            return (BuildIntegrity - CurrentDamage) / MaxIntegrity;
        }

        private void BuildExtention(StringBuilder sb)
        {
            sb.AppendLine("Building Extention");
            sb.AppendLine(StateWelders(true));
            sb.AppendLine(StateMergeHolder(true));
            sb.AppendLine(StateGrinder(false));
            StateProjector(true);
            sb.AppendLine("Remaining Blocks: " + projector.RemainingBlocks);

            //If we've been building for longer than required length
            if (projector.RemainingBlocks == 0)
            {
                curState = MinerState.Descending;
            }
            sb.AppendLine();
            HaveRequiredComp(sb);
        }
        
        private void Descending(StringBuilder sb)
        {
            sb.AppendLine(StateWelders(true));
            sb.AppendLine(StateMergeHolder(true));
            sb.AppendLine(StateGrinder(false));
            StateProjector(false);

            extentionPistons.Descend(SpeedDescend1);

            if (extentionPistons.CurrentLength == 0)
            {
                descend1Timer.Restart();
            }
            else
            {
                //Has the pistons stopped and the holder is connected and we have crunched onto the main leg.
                if (extentionPistons.CurrentLength > DescendLength2Min && extentionPistons.CurrentLength < DescendLength2Max && extentionPistons.MovementState == PistonMovementState.Static && descend1Timer.TotalSeconds > descend1TimerLength)
                {
                    descend1Timer.Running = false;
                    SetState(MinerState.Descending2);
                }
                if (extentionPistons.CurrentLength > DescendLengthBottom && extentionPistons.MovementState == PistonMovementState.Static)
                {
                    SetState(MinerState.Descending2);
                }
                else
                {
                    sb.AppendLine("Timer: " + descend1Timer.TotalSeconds + "/" + descend1TimerLength);
                }
            }
        }

        private void Descending2(StringBuilder sb)
        {
            sb.AppendLine(StateWelders(false));
            sb.AppendLine(StateMergeHolder(false));
            sb.AppendLine(StateGrinder(false));
            StateProjector(false);
            extentionPistons.Descend(SpeedDescend2);
            //Wait till bottom
            if (extentionPistons.CurrentLength > DescendLengthBottom && extentionPistons.MovementState == PistonMovementState.Static)
            {
                SetState(MinerState.Grinding);
            }
        }
        
        private void Grinder(StringBuilder sb)
        {
            //Make sure holder is running and connected!
            if (holderMerger.IsConnected && holderMerger.Enabled == true)
            {
                sb.AppendLine(StateWelders(false));
                sb.AppendLine(StateMergeHolder(true));
                sb.AppendLine(StateGrinder(true));
                StateProjector(false);
                sb.AppendLine("Timer: " + grindTimer.TotalSeconds + "/" + grindTimerLength);
                sb.AppendLine("Grinding . . . ");

                //Start timer
                if (grindTimer.Running == false) grindTimer.Restart();
                if (grindTimer.TotalSeconds > grindTimerLength)
                {
                    grindTimer.Running = false;
                    SetState(MinerState.Retracting);
                }
            }
            else
            {

                sb.AppendLine(StateWelders(false));
                sb.AppendLine(StateMergeHolder(true));
                sb.AppendLine(StateGrinder(false));
                StateProjector(false);
                sb.AppendLine("Waiting for Holder To Connect. . . ");
            }
        }
        
        private void Retracting(StringBuilder sb)
        {
            sb.AppendLine(StateWelders(false));
            sb.AppendLine(StateMergeHolder(true));
            sb.AppendLine(StateGrinder(true));
            StateProjector(true);
            sb.AppendLine("Waiting for Pistins to Retract . . .");
            if(extentionPistons.CurrentLength > DescendLengthTop && extentionPistons.MovementState == PistonMovementState.Static)
            {
                extentionPistons.Ascend(SpeedAscend);
            }
            else
            {
                SetState(MinerState.BuildingExtention);
            }
        }

        private MinerState SolveState(StringBuilder sb)
        {
            sb.AppendLine("Calculating State");
            List<KeyValuePair<MinerState, IMyInteriorLight>> active = new List<KeyValuePair<MinerState, IMyInteriorLight>>();
            foreach(var l in minerStateLights)
            {
                if (l.Value.Enabled)
                {
                    sb.AppendLine("Light " + l.Key + ": " + l.Value.Enabled.ToString());
                    active.Add(new KeyValuePair<MinerState, IMyInteriorLight>(MinerState.AllStates[l.Key], l.Value));
                }
            }
            //Order the states and go for min
            MinerState newState;
            //If we have no active, lets assume We're are the start, BUILD!
            if (active.Count == 0)
            {
                sb.AppendLine("None Active - Setting to Build Extention");
                newState = MinerState.BuildingExtention;
            }
            else
            {
                newState = active.OrderBy(d => d.Key.Order).First().Key;
                sb.AppendLine("New State " + newState.State);
            }
            SetState(newState);
            return newState;
        }
        
        private void SetState(MinerState State)
        {
            Echo("Set State " + State.State);
            curState = State;
            //Set Light
            if (!(State.State == MinerState.Init.State || State.State == MinerState.Init2.State || State.State == MinerState.Init3.State))
            {
                foreach (var l in minerStateLights)
                {
                    l.Value.Enabled = false;
                }
                minerStateLights[State.State].Enabled = true;
            }
        }
    }
}