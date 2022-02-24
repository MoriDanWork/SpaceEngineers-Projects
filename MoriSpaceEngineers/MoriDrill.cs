using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using VRageMath;

namespace MoriSpaceEngineers
{
    public sealed class Program : MyGridProgram
    {
        // Замедление ротороа
        // Остановку


        float upperHorizontal = -1f; // Turn for horizontal rotor
        float lowerHorizontal = -90f;
        float openPistonVelocity = 0.3f; // Open Piston Speed
        float closePistonVelocity = -3f; // Close Piston Speed

        Color colorYellow = new Color(255, 255, 0); //Color Yellow
        Color colorRed = new Color(255, 0, 0); //Color Red
        Color colorGreen = new Color(0, 255, 0);

        static string drillStatus = "Loading..";
        static string jibStatus = "Loading..";
        static string pistonStatus = "Loading..";
        static string drillDeep = "Loading..";
        static string everyStatus = "Loading..";
        static string moriLCDText = "========MoriDrill========" +
            $"\n Статус бурильщика: {everyStatus}" +
            $"\n Статус ротора: {jibStatus}" +
            $"\n Глубина погружения: {drillDeep}" +
            $"\n Буры: {drillStatus}" +
            $"\n Статус поршней: {pistonStatus}";

        IMyLightingBlock checkLight;
        IMyLightingBlock alertLight;
        //------------BEGIN--------------

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string args)
        {
            checkLight = GridTerminalSystem.GetBlockWithName("DrillLight") as IMyLightingBlock;
            alertLight = GridTerminalSystem.GetBlockWithName("AlertLight") as IMyLightingBlock;
            ((IMyMotorAdvancedStator)GridTerminalSystem.GetBlockWithName("DrillRotor")).SetValue<float>("Velocity", -1.9f);
            var LCD = GridTerminalSystem.GetBlockWithName("MoriLCD") as IMyTextPanel;

            LCD.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
            LCD.WriteText(moriLCDText);

            LCD.WriteText(UpdateLCD());

            if (checkLight.IsWorking)
            {
                alertLight.SetValue<Color>("Color", colorGreen);
                everyStatus = "Работает..";
                StartWork();
            }
            else
            {
                if (StopWork())
                {
                    alertLight.SetValue<Color>("Color", colorYellow);
                    everyStatus = "В простое..";
                }
                else
                {
                    alertLight.SetValue<Color>("Color", colorRed);
                    everyStatus = "В процессе остановки..";
                }
            }
        }

        public void StartWork()
        {
            if (SpinRotor(true))
            {
                if (PistonControl(true))
                {
                    checkLight.Enabled = false;
                    StopWork();
                }
                else
                {
                    ((IMyMotorAdvancedStator)GridTerminalSystem.GetBlockWithName("DrillRotor")).RotorLock = false;
                    ((IMyMotorAdvancedStator)GridTerminalSystem.GetBlockWithName("DrillRotor")).SetValue<float>("Velocity", -4f);
                    DrillEnable(true);
                }
            }
        }

        public bool StopWork()
        {
            ((IMyLightingBlock)GridTerminalSystem.GetBlockWithName("DrillLight")).Enabled = false;
            ((IMyMotorAdvancedStator)GridTerminalSystem.GetBlockWithName("DrillRotor")).SetValue<float>("Velocity", 0f);
            ((IMyMotorAdvancedStator)GridTerminalSystem.GetBlockWithName("DrillRotor")).RotorLock = true;
            DrillEnable(false);
            if (PistonControl(false))
            {
               
                if (SpinRotor(false)) return true;
            }
            return false;
        }

        public bool SpinRotor(bool Status)
        {
            var Rotor = GridTerminalSystem.GetBlockWithName("HorizontalRotor") as IMyMotorAdvancedStator;
            Rotor.SetValue<float>("UpperLimit", upperHorizontal);
            Rotor.SetValue<float>("LowerLimit", lowerHorizontal);

            float currentAngle = Rotor.Angle / (float)Math.PI * 180f;

            if (Status)
            {
                Rotor.SetValue<float>("Velocity", -3f);
                if (currentAngle <= lowerHorizontal)
                {
                    return true;
                }
            }
            else
            {
                Rotor.SetValue<float>("Velocity", 3f);
                if (currentAngle >= upperHorizontal)
                {
                    return true;
                }

            }

            Echo(currentAngle.ToString());
            return false;
        }

        public bool PistonControl(bool Status)
        {
            var pistons = new List<IMyPistonBase>();
            GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(pistons, (x) => x.CustomName == "DrillPiston");

            for (int i = 0; i < pistons.Count; ++i)
            {
                if (Status)
                {
                    pistons[i].Velocity = openPistonVelocity;
                    if (pistons[i].Status == PistonStatus.Extended)
                    {
                        pistons[i].Velocity = 0.1f;
                    }
                    else break;
                }
                else
                {
                    pistons[i].Velocity = closePistonVelocity;
                    if (pistons[i].Status != PistonStatus.Retracted)
                    {
                        pistons[i].Velocity = -0.1f;
                    }
                }
            }
            if (Status)
            {
                if (pistons[pistons.Count - 1].Status == PistonStatus.Extended) return true;
                return false;
            }
            else
                if (pistons.Exists(x => x.Status != PistonStatus.Retracted)) return false;
            return true;

        }


        public void DrillEnable(bool drillIsEnabled)
        {
            var drills = new List<IMyShipDrill>();
            GridTerminalSystem.GetBlocksOfType<IMyShipDrill>(drills, (x) => x.CustomName == "MoriDrill");
            foreach (var drill in drills)
            {
                drill.Enabled = drillIsEnabled;
            }
        }


        public void GetRotorStatus()
        {
            var Rotor = GridTerminalSystem.GetBlockWithName("HorizontalRotor") as IMyMotorAdvancedStator;
            float currentAngle = Rotor.Angle / (float)Math.PI * 180f;
            if (System.Math.Round(currentAngle / lowerHorizontal * 100) <= 1)
            {
                jibStatus = "Сложена";
            }
            else if (System.Math.Round(currentAngle / lowerHorizontal * 100) >= 100)
                jibStatus = "Разложена";
            else
                jibStatus = $"{System.Math.Round(currentAngle / lowerHorizontal * 100)}%";
        }

        public void GetPistonStatus()
        {
            string Status = "";
            var pistons = new List<IMyPistonBase>();
            GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(pistons, (x) => x.CustomName == "DrillPiston");

            for (int i = 0; i < pistons.Count; ++i)
            {
                Status += $"\n   Поршень[{i + 1}] - {pistons[i].Status}";
            }

            pistonStatus = Status;
        }

        public void GetDrillsStatus()
        {
            var drills = new List<IMyShipDrill>();
            GridTerminalSystem.GetBlocksOfType<IMyShipDrill>(drills, (x) => x.CustomName == "MoriDrill");
            if (drills.Exists(x => x.Enabled == true))
            {
                drillStatus = "Работают";
            }
            else
            {
                drillStatus = "Выключены";
            }
        }

        public void GetDrillDeep()
        {
            var pistons = new List<IMyPistonBase>();
            GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(pistons, (x) => x.CustomName == "DrillPiston");
            float accum = 0;

            foreach (var piston in pistons)
            {
                var pistonInf = piston.DetailedInfo;
                string[] pistInfArr = (pistonInf.Split(':'));
                string[] pistonDist = (pistInfArr[1].Split('m'));
                accum += (float)double.Parse(pistonDist[0]);
            }

            drillDeep = accum + "m / " + 10 * pistons.Count + "m";
        }

        public string UpdateLCD()
        {
            GetRotorStatus();
            GetDrillsStatus();
            GetPistonStatus();
            GetDrillDeep();

            return "========MoriDrill========" +
            $"\n Статус бурильщика: {everyStatus}" +
            $"\n Статус стрелы: {jibStatus}" +
            $"\n Глубина погружения: {drillDeep}" +
            $"\n Буры: {drillStatus}" +
            $"\n Статус поршней: {pistonStatus}";
        }
    }
}
