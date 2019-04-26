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
        int shootFrequencyMs = -1;
        string strShootFrequencyMs;

        int rocketCounter = 0;
        int runCounter = 0;
        double msSinceLastShot;
        List<IMySmallMissileLauncher> rocketList;

        public Program()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            //     
            // The constructor is optional and can be removed if not
            // needed.
            // 
            // It's recommended to set RuntimeInfo.UpdateFrequency 
            // here, which will allow your script to run itself without a 
            // timer block.
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            rocketList = GetRockets();
            CheckShootFrequency();
            msSinceLastShot = 99999;
        }

        private void CheckShootFrequency()
        {
            string tmp = Me.CustomData;
            if (tmp != strShootFrequencyMs)
            {
                strShootFrequencyMs = tmp;
                if (!int.TryParse(Me.CustomData, out shootFrequencyMs))
                    shootFrequencyMs = 100;
            }
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
            msSinceLastShot += Runtime.TimeSinceLastRun.TotalMilliseconds;
            if (msSinceLastShot > shootFrequencyMs)
            {
                msSinceLastShot = 0;
                try
                {
                    if (rocketCounter >= rocketList.Count)
                    {
                        rocketCounter = 0;
                    }

                    var b = rocketList[rocketCounter];
                    Echo("Shoot Frequency: " + shootFrequencyMs + "ms");
                    Echo("(" + (rocketCounter + 1) + "/" + rocketList.Count + ") Shooting " + b.CustomName);
                    b.ApplyAction("ShootOnce");
                    rocketCounter++;
                    runCounter++;
                }
                catch
                {
                    Echo("**********************Error!");
                    Echo(rocketCounter.ToString());
                    rocketList = GetRockets();
                    rocketCounter = 0;
                }
            }
            /*
            List<ITerminalAction> action = new List<ITerminalAction>();
            rocketList[0].GetActions(action);
            Echo(rocketList.Count.ToString());
            foreach (var a in action)
            {
                Echo(a.Name.ToString() + " - " + a.Id.ToString());
            }
            */
        }

        public List<IMySmallMissileLauncher> GetRockets()
        {
            CheckShootFrequency();
               var tmpList = new List<IMySmallMissileLauncher>();
            GridTerminalSystem.GetBlocksOfType<IMySmallMissileLauncher>(tmpList, d => d.Enabled && d.IsSameConstructAs(this.Me));
            return tmpList;
        }
    }
}