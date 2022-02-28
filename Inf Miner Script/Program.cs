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
        public float DescendLengthBottom = 3.8f;


        Dictionary<string, string> TypeToBlueprint;
        Dictionary<string, int> TypeToRequired;
        double limit = 90;

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
            public static MinerState BuildComp => new MinerState { Order = 0, State = "Building Components" };
            public static MinerState BuildingExtention => new MinerState { Order = 0, State = "Building Extention" };
            public static MinerState Descending => new MinerState { Order = 0, State = "Descending" };
            public static MinerState Descending2 => new MinerState { Order = 0, State = "Descending 2" };
            public static MinerState DrillClearBottom => new MinerState { Order = 0, State = "Drill Clear Bottom" };
            public static MinerState Grinding => new MinerState { Order = 0, State = "Grinding" };
            public static MinerState Retracting => new MinerState { Order = 0, State = "Retracting" };

            public static Dictionary<string, MinerState> AllStates = new Dictionary<string, MinerState>
            {
                { Init.State, Init },
                { Init2.State, Init2 },
                { Init3.State, Init3 },
                { BuildComp.State, BuildComp },
                { BuildingExtention.State, BuildingExtention },
                { Descending.State, Descending },
                { Descending2.State, Descending2 },
                { DrillClearBottom.State, DrillClearBottom },
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

        Timer buildTimer = new Timer();
        double builtTimerLength = 10;

        Timer grindTimer = new Timer();
        double grindTimerLength = 10;

        Timer descend1Timer = new Timer();
        double descend1TimerLength = 1;

        Timer clearTimer = new Timer();
        double clearTimerLength = 30;

        float startClearingRotation = -1000;
        //Timer clearTimer = new Timer();
        //double clearTimerLength = 3000;
        private bool clearFlipped;

        string offLightString = prefix + "Light Off";
        IMyInteriorLight offLight;
        string onLightString = prefix + "Light On";
        IMyInteriorLight onLight;

        string projectorString = prefix + "Projector";
        IMyProjector projector;

        string invCompString = prefix + "Cargo Comp";
        IMyInventory invComp;

        string invStoneString = prefix + "Cargo Stone";
        IMyInventory invStone;

        string cargoIngotsString = prefix + "Cargo Ingots";
        IMyInventory invIngots;

        string assemblerString = prefix + "Assembler";
        IMyAssembler assembler;
        IMyInventory invAssembler;

        string minerStateLightsString = prefix + "Light State ";
        Dictionary<string, IMyInteriorLight> minerStateLights = new Dictionary<string, IMyInteriorLight>();

        public Program()
        {
            StringBuilder sb = new StringBuilder();
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
                { "MyObjectBuilder_BlueprintDefinition/ConstructionComponent", 31*1 },
                { "MyObjectBuilder_BlueprintDefinition/InteriorPlate", 22*1 },
                { "MyObjectBuilder_BlueprintDefinition/SteelPlate", 11*1 },
                { "MyObjectBuilder_BlueprintDefinition/SmallTube", 3*1 },
                { "MyObjectBuilder_BlueprintDefinition/MotorComponent", 16*1 },
                { "MyObjectBuilder_BlueprintDefinition/ComputerComponent", 3*1 },
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
                else if (b.CustomName == assemblerString)
                {
                    assembler = b as IMyAssembler;
                    invAssembler = b.GetInventory(1);
                    sb.AppendLine("Found Assembler");
                }
                else if (b.CustomName == invCompString)
                {
                    invComp = b.GetInventory(0) as IMyInventory;
                    sb.AppendLine("Found Cargo Comp");
                }
                else if (b.CustomName == invStoneString)
                {
                    invStone = b.GetInventory(0) as IMyInventory;
                    sb.AppendLine("Found Cargo Stone");
                }
                else if (b.CustomName == cargoIngotsString)
                {
                    invIngots = b.GetInventory(0) as IMyInventory;
                    sb.AppendLine("Found Cargo Ingots");
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
            if (invStone == null)
            {
                envTest = false;
                sb.AppendLine("Cargo Stone not Found");
            }
            if (invComp == null)
            {
                envTest = false;
                sb.AppendLine("Cargo Comp not Found");
            }
            if (assembler == null || invAssembler == null)
            {
                envTest = false;
                sb.AppendLine("Assembler not Found");
            }
            if (invIngots == null)
            {
                envTest = false;
                sb.AppendLine("Cargo Ingots not Found");
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
            SortCargo();
            sb.AppendLine("Running Main " + DateTime.Now);


            if (onLight.Enabled == true)
            {
                sb.Append("State: ").AppendLine(curState.State);
                sb.AppendLine(extentionPistons.UpdatePistons(curState));
                sb.AppendLine(StateDrillRotor(true));
                StateDrills(true);

                var perComp = Math.Round((((double)invComp.CurrentVolume) / ((double)invComp.MaxVolume)) * 100, 0);
                var perStone = Math.Round((((double)invStone.CurrentVolume) / ((double)invStone.MaxVolume)) * 100, 0);
                var perIngot = Math.Round((((double)invIngots.CurrentVolume) / ((double)invIngots.MaxVolume)) * 100, 0);
                
                if (perComp > limit
                    || (perStone > limit)
                    || (perIngot > limit))
                {
                    limit = 50;
                    if (invIngots.IsFull) sb.AppendLine("Ingot Cargo Full - Pausing - " + perIngot);
                    if (invComp.IsFull) sb.AppendLine("Components Cargo Full - Pausing - " + perComp);
                    if (invStone.IsFull) sb.AppendLine("Stone Cargo Full - Pausing - " + perStone);
                    sb.AppendLine(StateWelders(false));
                    sb.AppendLine(StateGrinder(false));
                    sb.AppendLine(StateDrills(false));
                    sb.AppendLine(StateDrillRotor(false));
                    extentionPistons.Descend(0);
                }
                else
                {
                    limit = 90;
                    if (curState.State == MinerState.Init.State) {
                        startClearingRotation = -1000;
                        SetState(MinerState.Init2);
                    }
                    else if (curState.State == MinerState.Init2.State)
                    {
                        SetState(MinerState.Init3);
                    }
                    else if (curState.State == MinerState.Init3.State)
                    {
                        SolveState(sb);
                    }
                    else if (curState.State == MinerState.BuildComp.State)
                    {
                        BuildComp(sb);
                    }
                    else if (curState.State == MinerState.BuildingExtention.State)
                    {
                        BuildExtention(sb);
                    }
                    else if (curState.State == MinerState.Descending.State)
                    {
                        Descending(sb);
                    }
                    else if (curState.State == MinerState.Descending2.State)
                    {
                        Descending2(sb);
                    }
                    else if (curState.State == MinerState.DrillClearBottom.State)
                    {
                        DrillClearBottom(sb);
                    }
                    else if (curState.State == MinerState.Grinding.State)
                    {
                        Grinder(sb);
                    }
                    else if (curState.State == MinerState.Retracting.State)
                    {
                        Retracting(sb);
                    }
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
                SetState(MinerState.Init);
            }
            Echo(sb.ToString());
            output.WriteText(sb.ToString());
            
        }

        private bool HaveRequiredComp(StringBuilder sb)
        {
            MyInventoryItem i;
            List<MyInventoryItem> items = new List<MyInventoryItem>();
            invComp.GetItems(items);
            Dictionary<string, int> itemsCol = new Dictionary<string, int>();
            int amount;
            string type;
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

        private void SortCargo() 
        {
            SortCargo(invComp, "MyObjectBuilder_Component");
            SortCargo(invIngots, "MyObjectBuilder_Ingot");
            SortCargo(invStone, "MyObjectBuilder_Ore");
            SortCargo(invAssembler, "");
        }

        private void SortCargo(IMyInventory inv, string notMove)
        {
            List<MyInventoryItem> items = new List<MyInventoryItem>();
            MyInventoryItem item;
            inv.GetItems(items);
            for (int k = 0; k < items.Count; k++)
            {
                item = items[k];
                if (item.Type.TypeId == "MyObjectBuilder_Component" && item.Type.TypeId != notMove)
                {
                    inv.TransferItemTo(invComp, item, item.Amount);
                }
                else if (item.Type.TypeId == "MyObjectBuilder_Ingot" && item.Type.TypeId != notMove)
                {
                    inv.TransferItemTo(invIngots, item, item.Amount);
                }
                else if (item.Type.TypeId == "MyObjectBuilder_Ore" && item.Type.TypeId != notMove)
                {
                    inv.TransferItemTo(invStone, item, item.Amount);
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

        private void BuildComp(StringBuilder sb)
        {
            sb.AppendLine("Building Extention");
            sb.AppendLine(StateWelders(true));
            sb.AppendLine(StateMergeHolder(true));
            sb.AppendLine(StateGrinder(false));
            StateProjector(true);

            if (!HaveRequiredComp(sb))
            {
                sb.AppendLine("Waiting for Required Components . . .");
            }
            else
            {
                buildTimer.Running = false;
                SetState(MinerState.BuildingExtention);
                sb.AppendLine("Moving To Buildign Exten");
            }
        }
        private void BuildExtention(StringBuilder sb)
        {
            sb.AppendLine("Building Extention");
            sb.AppendLine(StateWelders(true));
            sb.AppendLine(StateMergeHolder(true));
            sb.AppendLine(StateGrinder(false));
            StateProjector(true);
            
            if (buildTimer.Running == false) buildTimer.Restart();
            else
            {
                //If we've been building for longer than required length
                if (buildTimer.TotalSeconds > builtTimerLength)
                {
                    buildTimer.Running = false;
                    curState = MinerState.Descending;
                }
                else
                {
                    sb.AppendLine("Timer: " + buildTimer.TotalSeconds + "/" + builtTimerLength);
                }
            }
        }
        
        private void Descending(StringBuilder sb)
        {
            sb.AppendLine(StateWelders(false));
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

            //Descend phase 2, release holder as the top is connected and go ALLL the way down.
            if (holderMerger.Enabled == true) holderMerger.Enabled = false;
            if (welders[0].Enabled == true) foreach (var w in welders) w.Enabled = false;
            if (grinder.Enabled == true) grinder.Enabled = false;

            //Wait till bottom
            if (extentionPistons.CurrentLength > 3.8 && extentionPistons.MovementState == PistonMovementState.Static)
            {
                SetState(MinerState.DrillClearBottom);
            }
        }

        private void DrillClearBottom(StringBuilder sb)
        {
            sb.AppendLine(StateWelders(false));
            sb.AppendLine(StateMergeHolder(true));
            sb.AppendLine(StateGrinder(false));
            StateProjector(false);
            sb.AppendLine("Waiting for Rototaion of Drill To Clear Bottom of Pit");
            sb.AppendLine("Timer: " + clearTimer.TotalSeconds + " / " + clearTimerLength);
            if (startClearingRotation == -1000)
            {
                startClearingRotation = drillRotor.Angle;
                clearTimer.Restart();
                clearFlipped = false;
            }
            sb.AppendLine("Start Angle: " + startClearingRotation);
            double percent;
            double moved = drillRotor.Angle - startClearingRotation;
            if (clearFlipped == false)
            {
                if (moved < 0)
                {
                    clearFlipped = true;
                    moved = (2 * Math.PI - startClearingRotation) + drillRotor.Angle;
                }
            }
            else
            {
                moved = (2 * Math.PI - startClearingRotation) + drillRotor.Angle;
                if (drillRotor.Angle > startClearingRotation) moved = 2 * Math.PI + .1;
            }

            percent = Math.Round((moved / (Math.PI * 2)) * 100, 0);

            sb.AppendLine("Rotor Angle: " + Math.Round(drillRotor.Angle, 2) + " (" + Math.Round(moved, 2) + " | " + percent + "%)");
            if (clearTimer.TotalSeconds > clearTimerLength || moved >= (2 * Math.PI))
            {
                startClearingRotation = -1000;
                clearTimer.Running = false;
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
            if(extentionPistons.CurrentLength > 0 && extentionPistons.MovementState == PistonMovementState.Static)
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
                newState = MinerState.BuildComp;
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