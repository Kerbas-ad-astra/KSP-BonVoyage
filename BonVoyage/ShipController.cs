using KSP.Localization;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BonVoyage
{
    /// <summary>
    /// Ship controller. Child of BVController
    /// </summary>
    internal class ShipController : BVController
    {
        #region internal properties

        internal override double AverageSpeed { get { return ((angle <= 90) || (batteries.UseBatteries && (batteries.CurrentEC > 0)) ? (averageSpeed * speedMultiplier) : (averageSpeedAtNight * speedMultiplier)); } }

        #endregion


        #region Private properties

        // Config values
        private double averageSpeed = 0;
        private double averageSpeedAtNight = 0;
        private bool manned = false;
        // Config values

        private double speedMultiplier;
        private double angle; // Angle between the main body and the main sun
        private double electricPower_Solar; // Electric power from solar panels
        private double electricPower_Other; // Electric power from other power sources
        private double requiredPower; // Power required by engines
        private double maxSpeedBase; // maximum speed without modifiers
        private int crewSpeedBonus; // Speed modifier based on the available crew
        EngineTestResult engineTestResult = new EngineTestResult(); // Result of a test of wheels

        // Reduction of speed based on difference between required and available power in percents
        private double SpeedReduction
        {
            get
            {
                double speedReduction = 0;
                if (requiredPower > (electricPower_Solar + electricPower_Other))
                    speedReduction = (requiredPower - (electricPower_Solar + electricPower_Other)) / requiredPower * 100;
                return speedReduction;
            }
        }

        #endregion


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="v"></param>
        /// <param name="module"></param>
        internal ShipController(Vessel v, ConfigNode module) : base(v, module)
        {
            // Load values from config if it isn't the first run of the mod (we are reseting vessel on the first run)
            if (!Configuration.FirstRun)
            {
                averageSpeed = double.Parse(BVModule.GetValue("averageSpeed") != null ? BVModule.GetValue("averageSpeed") : "0");
                averageSpeedAtNight = double.Parse(BVModule.GetValue("averageSpeedAtNight") != null ? BVModule.GetValue("averageSpeedAtNight") : "0");
                manned = bool.Parse(BVModule.GetValue("manned") != null ? BVModule.GetValue("manned") : "false");
            }

            speedMultiplier = 1.0;
            angle = 0;
            electricPower_Solar = 0;
            electricPower_Other = 0;
            requiredPower = 0;
            maxSpeedBase = 0;
            crewSpeedBonus = 0;
        }


        /// <summary>
        /// Get controller type
        /// </summary>
        /// <returns></returns>
        internal override int GetControllerType()
        {
            return 1;
        }

        #region Status window texts

        internal override List<DisplayedSystemCheckResult> GetDisplayedSystemCheckResults()
        {
            base.GetDisplayedSystemCheckResults();

            DisplayedSystemCheckResult result = new DisplayedSystemCheckResult
            {
                Toggle = false,
                Label = Localizer.Format("#LOC_BV_Control_AverageSpeed"),
                Text = averageSpeed.ToString("F") + " m/s",
                Tooltip =
                    averageSpeed > 0
                    ?
                    Localizer.Format("#LOC_BV_Control_SpeedBase") + ": " + maxSpeedBase.ToString("F") + " m/s\n"
                        + (manned ? Localizer.Format("#LOC_BV_Control_DriverBonus") + ": " + crewSpeedBonus.ToString() + "%\n" : Localizer.Format("#LOC_BV_Control_UnmannedPenalty") + ": 80%\n")
                        + (SpeedReduction > 0 ? Localizer.Format("#LOC_BV_Control_PowerPenalty") + ": " + (SpeedReduction > 75 ? "100" : SpeedReduction.ToString("F")) + "%\n" : "")
                        + Localizer.Format("#LOC_BV_Control_SpeedAtNight") + ": " + averageSpeedAtNight.ToString("F") + " m/s"
                    :
                    Localizer.Format("#LOC_BV_Control_WheelsNotOnline")
            };
            displayedSystemCheckResults.Add(result);

            result = new DisplayedSystemCheckResult
            {
                Toggle = false,
                Label = Localizer.Format("#LOC_BV_Control_GeneratedPower"),
                Text = (electricPower_Solar + electricPower_Other).ToString("F"),
                Tooltip = Localizer.Format("#LOC_BV_Control_SolarPower") + ": " + electricPower_Solar.ToString("F") + "\n" + Localizer.Format("#LOC_BV_Control_GeneratorPower") + ": " + electricPower_Other.ToString("F") + "\n"
                    + Localizer.Format("#LOC_BV_Control_UseBatteries_Usage") + ": " + (batteries.UseBatteries ? (batteries.MaxUsedEC.ToString("F0") + " / " + batteries.MaxAvailableEC.ToString("F0") + " EC") : Localizer.Format("#LOC_BV_Control_No"))
            };
            displayedSystemCheckResults.Add(result);

            result = new DisplayedSystemCheckResult
            {
                Toggle = false,
                Label = Localizer.Format("#LOC_BV_Control_RequiredPower"),
                Text = requiredPower.ToString("F")
                    + (SpeedReduction == 0 ? "" :
                        (((SpeedReduction > 0) && (SpeedReduction <= 75))
                            ? " (" + Localizer.Format("#LOC_BV_Control_PowerReduced") + " " + SpeedReduction.ToString("F") + "%)"
                            : " (" + Localizer.Format("#LOC_BV_Control_NotEnoughPower") + ")")),
                Tooltip = ""
            };
            displayedSystemCheckResults.Add(result);

            result = new DisplayedSystemCheckResult
            {
                Toggle = true,
                Text = Localizer.Format("#LOC_BV_Control_UseBatteries"),
                Tooltip = Localizer.Format("#LOC_BV_Control_UseBatteries_Tooltip"),
                GetToggleValue = GetUseBatteries,
                ToggleSelectedCallback = UseBatteriesChanged
            };
            displayedSystemCheckResults.Add(result);

            result = new DisplayedSystemCheckResult
            {
                Toggle = true,
                Text = Localizer.Format("#LOC_BV_Control_UseFuelCells"),
                Tooltip = Localizer.Format("#LOC_BV_Control_UseFuelCellsTooltip"),
                GetToggleValue = GetUseFuelCells,
                ToggleSelectedCallback = UseFuelCellsChanged
            };
            displayedSystemCheckResults.Add(result);

            return displayedSystemCheckResults;
        }

        #endregion

        #region Pathfinder

        /// <summary>
        /// Find a route to the target
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <returns></returns>
        internal override bool FindRoute(double lat, double lon)
        {
            return FindRoute(lat, lon, TileTypes.Ocean);
        }

        #endregion



        /// <summary>
        /// Check the systems
        /// </summary>
        internal override void SystemCheck()
        {
            base.SystemCheck();

            // Crawl for engines

            engineTestResult = CheckEngines();



            // Generally, moving at high speed requires less power than wheels' max consumption. Maximum required power of controller will 35% of wheels power requirement 
            requiredPower = engineTestResult.powerRequired / 100 * 35;

            // Get power production
            electricPower_Solar = GetAvailablePower_Solar();
            electricPower_Other = GetAvailablePower_Other();

            // Get available EC from batteries
            if (batteries.UseBatteries)
                batteries.MaxAvailableEC = GetAvailableEC_Batteries();
            else
                batteries.MaxAvailableEC = 0;

            // Get available EC from fuell cells
            fuelCells.OutputValue = 0;
            fuelCells.InputResources.Clear();
            if (fuelCells.Use)
            {
                List<ModuleResourceConverter> mrc = vessel.FindPartModulesImplementing<ModuleResourceConverter>();
                for (int i = 0; i < mrc.Count; i++)
                {
                    bool found = false;
                    try
                    {
                        var ec = mrc[i].outputList.Find(x => x.ResourceName == "ElectricCharge");
                        fuelCells.OutputValue = ec.Ratio;
                        found = true;
                    }
                    catch
                    {
                        found = false;
                    }

                    if (found)
                    {
                        // Add input resources
                        var iList = mrc[i].inputList;
                        for (int r = 0; r < iList.Count; r++)
                        {
                            var ir = fuelCells.InputResources.Find(x => x.Name == iList[r].ResourceName);
                            if (ir == null)
                            {
                                ir = new Resource();
                                ir.Name = iList[r].ResourceName;
                                fuelCells.InputResources.Add(ir);
                            }
                            ir.Ratio += iList[r].Ratio;
                        }
                    }
                }
            }
            electricPower_Other += fuelCells.OutputValue;

            // Manned
            manned = (vessel.GetCrewCount() > 0);

            // Pilots and Scouts (USI) increase base average speed
            crewSpeedBonus = 0;
            if (manned)
            {
                int maxPilotLevel = -1;
                int maxScoutLevel = -1;
                int maxDriverLevel = -1;

                List<ProtoCrewMember> crewList = vessel.GetVesselCrew();
                for (int i = 0; i < crewList.Count; i++)
                {
                    switch (crewList[i].trait)
                    {
                        case "Pilot":
                            if (maxPilotLevel < crewList[i].experienceLevel)
                                maxPilotLevel = crewList[i].experienceLevel;
                            break;
                        case "Scout":
                            if (maxScoutLevel < crewList[i].experienceLevel)
                                maxScoutLevel = crewList[i].experienceLevel;
                            break;
                        default:
                            if (crewList[i].HasEffect("AutopilotSkill"))
                                if (maxDriverLevel < crewList[i].experienceLevel)
                                    maxDriverLevel = crewList[i].experienceLevel;
                            break;
                    }
                }
                if (maxPilotLevel > 0)
                    crewSpeedBonus = 6 * maxPilotLevel; // up to 30% for a Pilot
                else if (maxDriverLevel > 0)
                    crewSpeedBonus = 4 * maxDriverLevel; // up to 20% for any driver (has AutopilotSkill skill)
                else if (maxScoutLevel > 0)
                    crewSpeedBonus = 2 * maxScoutLevel; // up to 10% for a Scout (Scouts disregard safety)
            }

            // Average speed will vary depending on number of wheels online and crew present from 50 to 95 percent of average wheels' max speed
            if (engineTestResult.online != 0)
            {
                maxSpeedBase = 18; // 35 knots = 18 m/s, pretty quick for a ship
                averageSpeed = maxSpeedBase * (1 + crewSpeedBonus / 100);
            }
            else
                averageSpeed = 0;

            // Unmanned rovers drive with 80% speed penalty
            if (!manned)
                averageSpeed = averageSpeed * 0.2;

            // Base average speed at night is the same as average speed, if there is other power source. Zero otherwise.
            if (electricPower_Other > 0.0)
                averageSpeedAtNight = averageSpeed;
            else
                averageSpeedAtNight = 0;

            // If required power is greater then total power generated, then average speed can be lowered up to 75%
            if (requiredPower > (electricPower_Solar + electricPower_Other))
            {
                double speedReduction = (requiredPower - (electricPower_Solar + electricPower_Other)) / requiredPower;
                if (speedReduction <= 0.75)
                    averageSpeed = averageSpeed * (1 - speedReduction);
            }

            // If required power is greater then other power generated, then average speed at night can be lowered up to 75%
            if (requiredPower > electricPower_Other)
            {
                double speedReduction = (requiredPower - electricPower_Other) / requiredPower;
                if (speedReduction <= 0.75)
                    averageSpeedAtNight = averageSpeedAtNight * (1 - speedReduction);
                else
                    averageSpeedAtNight = 0;
            }

            // If we are using batteries, compute for how long and how much EC we can use
            if (batteries.UseBatteries)
            {
                batteries.MaxUsedEC = 0;
                batteries.ECPerSecondConsumed = 0;
                batteries.ECPerSecondGenerated = 0;

                // We have enough of solar power to recharge batteries
                if (requiredPower < (electricPower_Solar + electricPower_Other))
                {
                    batteries.ECPerSecondConsumed = Math.Max(requiredPower - electricPower_Other, 0); // If there is more other power than required power, we don't need batteries
                    batteries.MaxUsedEC = batteries.MaxAvailableEC / 2; // We are using only half of max available EC
                    if (batteries.ECPerSecondConsumed > 0)
                    {
                        double halfday = vessel.mainBody.rotationPeriod / 2; // in seconds
                        batteries.ECPerSecondGenerated = electricPower_Solar + electricPower_Other - requiredPower;
                        batteries.MaxUsedEC = Math.Min(batteries.MaxUsedEC, batteries.ECPerSecondConsumed * halfday); // get lesser value of MaxUsedEC and EC consumed per night
                        batteries.MaxUsedEC = Math.Min(batteries.MaxUsedEC, batteries.ECPerSecondGenerated * halfday); // get lesser value of MaxUsedEC and max EC available for recharge during a day
                    }
                }

                batteries.CurrentEC = batteries.MaxUsedEC; // We are starting at full available capacity
            }
        }


        #region Wheels

        /// <summary>
        /// Result of the test of wheels
        /// </summary>
        private struct EngineTestResult
        {
            internal double powerRequired; // Total power required
            internal int online; // Count of online engines
            internal int electricFans; // Count of online engines which are electric fans

            internal EngineTestResult(double powerRequired, int online, int electricFans)
            {
                this.powerRequired = powerRequired;
                this.online = online;
                this.electricFans = electricFans;
            }
        }


        /// <summary>
        /// Test stock wheels implementing standard module ModuleWheelBase
        /// </summary>
        /// <returns></returns>
        private EngineTestResult CheckEngines()
        {
            double powerRequired = 0;
            int online = 0;
            int electricFans = 0;

            // Get wheel modules
            List<ModuleEngines> engines = new List<ModuleEngines>();
            for (int i = 0; i < vessel.parts.Count; i++)
            {
                var part = vessel.parts[i];
                ModuleEngines engine = new ModuleEngines();
                if (part.Modules.Contains("ModuleEngines"))
                {
                    engine = part.Modules["ModuleEngines"] as ModuleEngines;
                }
                else if (part.Modules.Contains("ModuleEnginesFX"))
                {
                    engine = part.Modules["ModuleEnginesFX"] as ModuleEngines;
                }
                else if (part.Modules.Contains("ModuleEnginesRF"))
                {
                    engine = part.Modules["ModuleEnginesRF"] as ModuleEngines;
                }
                else
                {
                    continue;
                }
            }

            for (int i = 0; i < engines.Count; i++)
            {
                var engine = engines[i];
                double enginePower = 0;

                if (engine.EngineIgnited)
                {

                    bool engineIsElectric = false;
                    bool engineIsFan = true;
                    bool engineIsSatisfied = true;

                    foreach (var propellant in engine.propellants)
                    {
                        if (propellant.name == "ElectricCharge")
                        {
                            engineIsElectric = true;
                            double fuelFlow = Mathf.Lerp(engine.minFuelFlow, engine.maxFuelFlow, engine.thrustPercentage / 100);
                            enginePower = fuelFlow * propellant.ratio / engine.ratioSum;
                        }
                        else if (propellant.name != "FSCoolant" || propellant.name != "IntakeAir" || propellant.name != "IntakeAtm" || propellant.name != "IntakeLqd")
                        {
                            engineIsFan = false;

                            if (propellant.totalResourceAvailable < 0)
                                engineIsSatisfied = false;
                        }
                    }

                    if (engineIsSatisfied)
                        online++;

                    if (engineIsElectric && engineIsFan)
                    {
                        electricFans++;
                        powerRequired += enginePower;
                    }
                }
            }

            return new EngineTestResult(powerRequired, online, electricFans);
        }

        #endregion


        #region Power

        /// <summary>
        /// Calculate available power from solar panels
        /// </summary>
        /// <returns></returns>
        private double GetAvailablePower_Solar()
        {
            // Kopernicus sets the right values for PhysicsGlobals.SolarLuminosity and PhysicsGlobals.SolarLuminosityAtHome so we can use them in all cases
            double solarPower = 0;
            double distanceToSun = Vector3d.Distance(vessel.GetWorldPos3D(), FlightGlobals.Bodies[mainStarIndex].position);
            double solarFlux = PhysicsGlobals.SolarLuminosity / (4 * Math.PI * distanceToSun * distanceToSun); // f = L / SA = L / 4π r2 (Wm-2)
            float multiplier = 1;

            for (int i = 0; i < vessel.parts.Count; ++i)
            {
                ModuleDeployableSolarPanel solarPanel = vessel.parts[i].FindModuleImplementing<ModuleDeployableSolarPanel>();
                if (solarPanel == null)
                    continue;

                if ((solarPanel.deployState != ModuleDeployablePart.DeployState.BROKEN) && (solarPanel.deployState != ModuleDeployablePart.DeployState.RETRACTED) && (solarPanel.deployState != ModuleDeployablePart.DeployState.RETRACTING))
                {
                    if (solarPanel.useCurve) // Power curve
                        multiplier = solarPanel.powerCurve.Evaluate((float)distanceToSun);
                    else // solar flux at current distance / solar flux at 1AU (Kerbin in stock, other value in Kopernicus)
                        multiplier = (float)(solarFlux / PhysicsGlobals.SolarLuminosityAtHome);
                    solarPower += solarPanel.chargeRate * multiplier;
                }
            }

            return solarPower;
        }


        /// <summary>
        /// Calculate available power from generators and reactors
        /// </summary>
        /// <returns></returns>
        private double GetAvailablePower_Other()
        {
            double otherPower = 0;

            // Go through all parts and get power from generators and reactors
            for (int i = 0; i < vessel.parts.Count; ++i)
            {
                var part = vessel.parts[i];

                // Standard RTG
                ModuleGenerator powerModule = part.FindModuleImplementing<ModuleGenerator>();
                if (powerModule != null)
                {
                    if (powerModule.generatorIsActive || powerModule.isAlwaysActive)
                    {
                        // Go through resources and get EC power
                        for (int j = 0; j < powerModule.resHandler.outputResources.Count; ++j)
                        {
                            var resource = powerModule.resHandler.outputResources[j];
                            if (resource.name == "ElectricCharge")
                                otherPower += resource.rate * powerModule.efficiency;
                        }
                    }
                }

                // Other generators
                PartModuleList modules = part.Modules;
                for (int j = 0; j < modules.Count; ++j)
                {
                    var module = modules[j];

                    // Near future fission reactors
                    if (module.moduleName == "FissionGenerator")
                        otherPower += double.Parse(module.Fields.GetValue("CurrentGeneration").ToString());

                    // KSP Interstellar generators
                    if ((module.moduleName == "ThermalElectricEffectGenerator") || (module.moduleName == "IntegratedThermalElectricPowerGenerator") || (module.moduleName == "ThermalElectricPowerGenerator")
                        || (module.moduleName == "IntegratedChargedParticlesPowerGenerator") || (module.moduleName == "ChargedParticlesPowerGenerator") || (module.moduleName == "FNGenerator"))
                    {
                        if (bool.Parse(module.Fields.GetValue("IsEnabled").ToString()))
                        {
                            //otherPower += double.Parse(module.Fields.GetValue("maxElectricdtps").ToString()); // Doesn't work as expected

                            string maxPowerStr = module.Fields.GetValue("MaxPowerStr").ToString();
                            double maxPower = 0;

                            if (maxPowerStr.Contains("GW"))
                                maxPower = double.Parse(maxPowerStr.Replace(" GW", "")) * 1000000;
                            else if (maxPowerStr.Contains("MW"))
                                maxPower = double.Parse(maxPowerStr.Replace(" MW", "")) * 1000;
                            else
                                maxPower = double.Parse(maxPowerStr.Replace(" KW", ""));

                            otherPower += maxPower;
                        }
                    }
                }

                // WBI reactors, USI reactors and MKS Power Pack
                ModuleResourceConverter converterModule = part.FindModuleImplementing<ModuleResourceConverter>();
                if (converterModule != null)
                {
                    if (converterModule.ModuleIsActive()
                        && ((converterModule.ConverterName == "Nuclear Reactor") || (converterModule.ConverterName == "Reactor") || (converterModule.ConverterName == "Generator")))
                    {
                        for (int j = 0; j < converterModule.outputList.Count; ++j)
                        {
                            var resource = converterModule.outputList[j];
                            if (resource.ResourceName == "ElectricCharge")
                                otherPower += resource.Ratio * converterModule.GetEfficiencyMultiplier();
                        }
                    }
                }
            }

            return otherPower;
        }


        /// <summary>
        /// Get maximum available EC from batteries
        /// </summary>
        /// <returns></returns>
        private double GetAvailableEC_Batteries()
        {
            double maxEC = 0;

            for (int i = 0; i < vessel.parts.Count; ++i)
            {
                var part = vessel.parts[i];
                if (part.Resources.Contains("ElectricCharge") && part.Resources["ElectricCharge"].flowState)
                    maxEC += part.Resources["ElectricCharge"].maxAmount;
            }

            return maxEC;
        }

        #endregion


        /// <summary>
        /// Activate autopilot
        /// </summary>
        internal override bool Activate()
        {
            if (vessel.situation != Vessel.Situations.SPLASHED)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BV_Warning_Splashed"), 5f).color = Color.yellow;
                return false;
            }

            SystemCheck();

            // At least 1 engine must be on
            if (engineTestResult.online < 1)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BV_Warning_EngineNotOnline"), 5f).color = Color.yellow;
                return false;
            }

            if (engineTestResult.electricFans < 1)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BV_Warning_EngineNotElectricFan"), 5f).color = Color.yellow;
                return false;
            }

            // Get fuel amount if fuel cells are used
            if (fuelCells.Use)
            {
                IResourceBroker broker = new ResourceBroker();
                var iList = fuelCells.InputResources;
                for (int i = 0; i < iList.Count; i++)
                {
                    iList[i].MaximumAmountAvailable = broker.AmountAvailable(vessel.rootPart, iList[i].Name, 1, ResourceFlowMode.ALL_VESSEL);

                    if (iList[i].MaximumAmountAvailable == 0)
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BV_Warning_NotEnoughFuel"), 5f).color = Color.yellow;
                        return false;
                    }
                }
            }

            // Power production
            if (requiredPower > (electricPower_Solar + electricPower_Other))
            {
                // If required power is greater than total power generated, then average speed can be lowered up to 75%
                double speedReduction = (requiredPower - (electricPower_Solar + electricPower_Other)) / requiredPower;

                if (speedReduction > 0.75)
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BV_Warning_LowPower"), 5f).color = Color.yellow;
                    return false;
                }
            }

            BonVoyageModule module = vessel.FindPartModuleImplementing<BonVoyageModule>();
            if (module != null)
            {
                module.averageSpeed = averageSpeed;
                module.averageSpeedAtNight = averageSpeedAtNight;
                module.manned = manned;
            }

            return base.Activate();
        }


        /// <summary>
        /// Deactivate autopilot
        /// </summary>
        internal override bool Deactivate()
        {
            SystemCheck();
            return base.Deactivate();
        }


        /// <summary>
        /// Update vessel
        /// </summary>
        /// <param name="currentTime"></param>
        internal override void Update(double currentTime)
        {
            if (vessel == null)
                return;
            if (vessel.isActiveVessel)
            {
                lastTimeUpdated = 0;
                if (active)
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BV_AutopilotActive"), 10f).color = Color.red;
                return;
            }

            if (!active || vessel.loaded)
                return;

            // If we don't know the last time of update, then set it and wait for the next update cycle
            if (lastTimeUpdated == 0)
            {
                State = VesselState.Idle;
                lastTimeUpdated = currentTime;
                BVModule.SetValue("lastTimeUpdated", currentTime.ToString());
                return;
            }

            Vector3d roverPos = vessel.mainBody.position - vessel.GetWorldPos3D();
            Vector3d toMainStar = vessel.mainBody.position - FlightGlobals.Bodies[mainStarIndex].position;
            angle = Vector3d.Angle(roverPos, toMainStar); // Angle between rover and the main star

            // Speed penalties at twighlight and at night
            if ((angle > 90) && manned) // night
                speedMultiplier = 0.25;
            else if ((angle > 85) && manned) // twilight
                speedMultiplier = 0.5;
            else if ((angle > 80) && manned) // twilight
                speedMultiplier = 0.75;
            else // day
                speedMultiplier = 1.0;

            double deltaT = currentTime - lastTimeUpdated; // Time delta from the last update
            double deltaTOver = 0; // deltaT which is calculated from a value over the maximum resource amout available

            // Compute increase or decrease in EC from the last update
            if (batteries.UseBatteries)
            {
                // Process fuel cells before batteries
                if (fuelCells.Use
                    && ((angle > 90)
                        || (batteries.ECPerSecondGenerated - fuelCells.OutputValue <= 0)
                        || (batteries.CurrentEC < batteries.MaxUsedEC))) // Night, not enough solar power or we need to recharge batteries
                {
                    var iList = fuelCells.InputResources;
                    for (int i = 0; i < iList.Count; i++)
                    {
                        iList[i].CurrentAmountUsed += iList[i].Ratio * deltaT;
                        if (iList[i].CurrentAmountUsed > iList[i].MaximumAmountAvailable)
                            deltaTOver = Math.Max(deltaTOver, (iList[i].CurrentAmountUsed - iList[i].MaximumAmountAvailable) / iList[i].Ratio);
                    }
                    if (deltaTOver > 0)
                    {
                        deltaT -= deltaTOver;
                        // Reduce the amount of used resources
                        for (int i = 0; i < iList.Count; i++)
                            iList[i].CurrentAmountUsed -= iList[i].Ratio * deltaTOver;
                    }
                }

                if (angle <= 90) // day
                    batteries.CurrentEC = Math.Min(batteries.CurrentEC + batteries.ECPerSecondGenerated * deltaT, batteries.MaxUsedEC);
                else // night
                    batteries.CurrentEC = Math.Max(batteries.CurrentEC - batteries.ECPerSecondConsumed * deltaT, 0);
            }

            // No moving at night, if there isn't enough power
            if ((angle > 90) && (averageSpeedAtNight == 0.0) && !(batteries.UseBatteries && (batteries.CurrentEC > 0)))
            {
                State = VesselState.AwaitingSunlight;
                lastTimeUpdated = currentTime;
                BVModule.SetValue("lastTimeUpdated", currentTime.ToString());
                return;
            }

            double deltaS = AverageSpeed * deltaT; // Distance delta from the last update
            distanceTravelled += deltaS;

            if (distanceTravelled >= distanceToTarget) // We reached the target
            {
                if (!MoveSafely(targetLatitude, targetLongitude))
                    distanceTravelled -= deltaS;
                else
                {
                    distanceTravelled = distanceToTarget;

                    active = false;
                    arrived = true;
                    BVModule.SetValue("active", "False");
                    BVModule.SetValue("arrived", "True");
                    BVModule.SetValue("distanceTravelled", distanceToTarget.ToString());
                    BVModule.SetValue("pathEncoded", "");

                    // Dewarp
                    if (Configuration.AutomaticDewarp)
                    {
                        if (TimeWarp.CurrentRate > 3) // Instant drop to 50x warp
                            TimeWarp.SetRate(3, true);
                        if (TimeWarp.CurrentRate > 0) // Gradual drop out of warp
                            TimeWarp.SetRate(0, false);
                        ScreenMessages.PostScreenMessage(vessel.vesselName + " " + Localizer.Format("#LOC_BV_VesselArrived") + " " + vessel.mainBody.bodyDisplayName.Replace("^N", ""), 5f);
                    }

                    NotifyArrival();
                }
                State = VesselState.Idle;
            }
            else
            {
                try // There is sometimes null ref exception during scene change
                {
                    int step = Convert.ToInt32(Math.Floor(distanceTravelled / PathFinder.StepSize)); // In which step of the path we are
                    double remainder = distanceTravelled % PathFinder.StepSize; // Current remaining distance from the current step
                    double bearing = 0;

                    if (step < path.Count - 1)
                        bearing = GeoUtils.InitialBearing( // Bearing to the next step from previous step
                            path[step].latitude,
                            path[step].longitude,
                            path[step + 1].latitude,
                            path[step + 1].longitude
                        );
                    else
                        bearing = GeoUtils.InitialBearing( // Bearing to the target from previous step
                            path[step].latitude,
                            path[step].longitude,
                            targetLatitude,
                            targetLongitude
                        );

                    // Compute new coordinates, we are moving from the current step, distance is "remainder"
                    double[] newCoordinates = GeoUtils.GetLatitudeLongitude(
                        path[step].latitude,
                        path[step].longitude,
                        bearing,
                        remainder,
                        vessel.mainBody.Radius
                    );

                    // Move
                    if (!MoveSafely(newCoordinates[0], newCoordinates[1]))
                    {
                        distanceTravelled -= deltaS;
                        State = VesselState.Idle;
                    }
                    else
                        State = VesselState.Moving;
                }
                catch { }
            }

            Save(currentTime);

            // Stop the rover, we don't have enough of fuel
            if (deltaTOver > 0)
            {
                active = false;
                arrived = true;
                BVModule.SetValue("active", "False");
                BVModule.SetValue("arrived", "True");
                BVModule.SetValue("pathEncoded", "");

                // Dewarp
                if (Configuration.AutomaticDewarp)
                {
                    if (TimeWarp.CurrentRate > 3) // Instant drop to 50x warp
                        TimeWarp.SetRate(3, true);
                    if (TimeWarp.CurrentRate > 0) // Gradual drop out of warp
                        TimeWarp.SetRate(0, false);
                    ScreenMessages.PostScreenMessage(vessel.vesselName + " " + Localizer.Format("#LOC_BV_Warning_Stopped") + ". " + Localizer.Format("#LOC_BV_Warning_NotEnoughFuel"), 5f).color = Color.red;
                }

                NotifyNotEnoughFuel();
                State = VesselState.Idle;
            }
        }


        /// <summary>
        /// Save move of a ship. We need to prevent hitting an active vessel.
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns>true if rover was moved, false otherwise</returns>
        private bool MoveSafely(double latitude, double longitude)
        {
            if (FlightGlobals.ActiveVessel != null)
            {
                Vector3d newPos = vessel.mainBody.GetWorldSurfacePosition(latitude, longitude, 0);
                Vector3d actPos = FlightGlobals.ActiveVessel.GetWorldPos3D();
                double distance = Vector3d.Distance(newPos, actPos);
                if (distance <= 2400)
                    return false;
            }

            vessel.latitude = latitude;
            vessel.longitude = longitude;

            return true;
        }


        /// <summary>
        /// Notify, that rover has arrived
        /// </summary>
        private void NotifyArrival()
        {
            MessageSystem.Message message = new MessageSystem.Message(
                Localizer.Format("#LOC_BV_Title_RoverArrived"), // title
                "<color=#74B4E2>" + vessel.vesselName + "</color> " + Localizer.Format("#LOC_BV_VesselArrived") + " " + vessel.mainBody.bodyDisplayName.Replace("^N", "") + ".\n<color=#AED6EE>"
                + Localizer.Format("#LOC_BV_Control_Lat") + ": " + targetLatitude.ToString("F2") + "</color>\n<color=#AED6EE>" + Localizer.Format("#LOC_BV_Control_Lon") + ": " + targetLongitude.ToString("F2") + "</color>", // message
                MessageSystemButton.MessageButtonColor.GREEN,
                MessageSystemButton.ButtonIcons.COMPLETE
            );
            MessageSystem.Instance.AddMessage(message);
        }


        /// <summary>
        /// Notify, that rover has not enough fuel
        /// </summary>
        private void NotifyNotEnoughFuel()
        {
            MessageSystem.Message message = new MessageSystem.Message(
                Localizer.Format("#LOC_BV_Title_RoverStopped"), // title
                "<color=#74B4E2>" + vessel.vesselName + "</color> " + Localizer.Format("#LOC_BV_Warning_Stopped") + ". " + Localizer.Format("#LOC_BV_Warning_NotEnoughFuel") + ".\n<color=#AED6EE>", // message
                MessageSystemButton.MessageButtonColor.RED,
                MessageSystemButton.ButtonIcons.ALERT
            );
            MessageSystem.Instance.AddMessage(message);
        }


        /// <summary>
        /// Return status of batteries usage
        /// </summary>
        /// <returns></returns>
        internal bool GetUseBatteries()
        {
            return batteries.UseBatteries;
        }


        /// <summary>
        /// Set batteries usage
        /// </summary>
        /// <param name="value"></param>
        internal void UseBatteriesChanged(bool value)
        {
            batteries.UseBatteries = value;
            if (!value)
                fuelCells.Use = false;
        }


        /// <summary>
        /// Return status of fuel cells usage
        /// </summary>
        /// <returns></returns>
        internal bool GetUseFuelCells()
        {
            return fuelCells.Use;
        }


        /// <summary>
        /// Set fuel cells usage
        /// </summary>
        /// <param name="value"></param>
        internal void UseFuelCellsChanged(bool value)
        {
            fuelCells.Use = value;
            if (value)
                batteries.UseBatteries = true;
        }

    }
}
