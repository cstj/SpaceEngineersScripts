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
        List<IMyAirVent> vents = new List<IMyAirVent>();
        List<IMyAirVent> ventsIn = new List<IMyAirVent>();
        double levelMax = 95;
        double levelMin = 80;
        double level;
        IMyTextSurface screen;

        public Program()
        {
            List<IMyAirVent> grid = new List<IMyAirVent>();
            GridTerminalSystem.GetBlocksOfType(grid);
            foreach (var v in grid)
            {
                Echo(v.CustomName);
                if (v.CustomName.ToLower().Contains("[auto]"))
                {
                    Echo("Match");
                    IMyAirVent ve = v as IMyAirVent;
                    if (ve != null)
                    {
                        if (ve.Depressurize == true) ventsIn.Add(ve);
                        else vents.Add(ve);
                    }
                }
            }
            if (vents.Count > 0)
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
                Echo("Found Vents");
            }
            else
            {
                Echo("Add [Auto] to some vents");
            }
            level = levelMin;
            screen = Me.GetSurface(0);
            screen.ContentType = ContentType.TEXT_AND_IMAGE;
        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            bool outvent = false;
            double oLevel;
            screen.WriteText("Auto Vents - " + level, false);
            if (vents.Count > 0)
            {
                foreach (var v in vents)
                {
                    oLevel = Math.Round(v.GetOxygenLevel() * 100,0);
                    if (v.CanPressurize == true && v.PressurizationEnabled == true)
                    {
                       
                        if (oLevel <= level)
                        {
                            level = levelMax;
                            outvent = true;
                        }
                        else
                        {
                            level = levelMin;
                            outvent = false;
                        }
                    }
                }
                if (outvent)
                {
                    screen.WriteText("\n\nIn Vents Enabled", true);
                    foreach (var v in ventsIn)
                    {
                        screen.WriteText("\n" + v.CustomName, true);
                        v.Enabled = true;
                    }
                }
                else
                {
                    screen.WriteText("\n\nIn Vents Disabled", true);
                    foreach(var v in ventsIn)
                    {
                        screen.WriteText("\n " + v.CustomName, true);
                        v.Enabled = false;
                    }
                }
            }
            else
            {
                Echo("Add [Auto] to some vents");
            }
        }
    }
}
