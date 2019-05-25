using KSP.Localization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BonVoyage
{
    /// <summary>
    /// Enum of vessel states
    /// </summary>
    internal enum VesselState
    {
        Idle = 0,
        ControllerDisabled = 1,
        Current = 2,
        Moving = 3,
        AwaitingSunlight = 4
    }


    /// <summary>
    /// Result for display in the Control Window
    /// </summary>
    internal struct DisplayedSystemCheckResult
    {
        internal string Label;
        internal string Text;
        internal string Tooltip;
        internal bool Toggle; // true - DialogGUIToggle ; false - DialogGUILabel
        internal Func<bool> GetToggleValue;
        internal Callback<bool> ToggleSelectedCallback;
    }


    /// <summary>
    /// Basic controller
    /// </summary>
    internal class BVController
    {
        #region internal properties

        internal Vessel vessel; // Vessel containing BonVoyageModule

        internal bool Shutdown
        {
            get { return shutdown; }
            set
            {
                shutdown = value;
                if (shutdown)
                    State = VesselState.ControllerDisabled;
                else
                    State = VesselState.Idle;
            }
        }

        internal bool Active {  get { return active; } }

        internal bool Arrived
        {
            get { return arrived; }
            set
            {
                arrived = value;
                BonVoyageModule module = vessel.FindPartModuleImplementing<BonVoyageModule>();
                if (module != null)
                    module.arrived = value;
            }
        }

        internal Vector3d RotationVector
        {
            get { return rotationVector; }
            set { rotationVector = value; }
        }

        internal double RemainingDistanceToTarget { get { return distanceToTarget - distanceTravelled; } }
        internal virtual double AverageSpeed { get { return 0; } }
        internal event EventHandler OnStateChanged;

        internal Batteries batteries = new Batteries(); // Information about batteries
        internal Converter fuelCells = new Converter(); // Information about fuel cells

        #endregion


        #region Private and protected properties

        protected ConfigNode BVModule; // Config node of BonVoyageModule
        protected List<DisplayedSystemCheckResult> displayedSystemCheckResults;
        protected int mainStarIndex; // Vessel's main star's index in the FlightGlobals.Bodies

        // Config values
        protected bool active = false;
        private bool shutdown = false;
        protected bool arrived = false;
        protected double targetLatitude = 0;
        protected double targetLongitude = 0;
        protected double distanceToTarget = 0;
        protected double distanceTravelled = 0;
        protected double lastTimeUpdated = 0;
        private Vector3d rotationVector = Vector3d.back; // Rotation of a craft
        // Config values

        internal List<PathUtils.WayPoint> path = null; // Path to destination

        private VesselState _state;
        internal VesselState State
        {
            get { return _state; }
            set
            {
                if (_state != value)
                {
                    _state = value;
                    if (OnStateChanged != null)
                        OnStateChanged(this, EventArgs.Empty);
                }
            }
        }

        #endregion


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="v"></param>
        /// <param name="module"></param>
        internal BVController(Vessel v, ConfigNode module)
        {
            vessel = v;
            BVModule = module;
            displayedSystemCheckResults = new List<DisplayedSystemCheckResult>();

            // Load values from config if it isn't the first run of the mod (we are reseting vessel on the first run)
            if (!Configuration.FirstRun)
            {
                active = bool.Parse(BVModule.GetValue("active") != null ? BVModule.GetValue("active") : "false");
                shutdown = bool.Parse(BVModule.GetValue("shutdown") != null ? BVModule.GetValue("shutdown") : "false");
                arrived = bool.Parse(BVModule.GetValue("arrived") != null ? BVModule.GetValue("arrived") : "false");
                targetLatitude = double.Parse(BVModule.GetValue("targetLatitude") != null ? BVModule.GetValue("targetLatitude") : "0");
                targetLongitude = double.Parse(BVModule.GetValue("targetLongitude") != null ? BVModule.GetValue("targetLongitude") : "0");
                distanceToTarget = double.Parse(BVModule.GetValue("distanceToTarget") != null ? BVModule.GetValue("distanceToTarget") : "0");
                distanceTravelled = double.Parse(BVModule.GetValue("distanceTravelled") != null ? BVModule.GetValue("distanceTravelled") : "0");
                if (BVModule.GetValue("pathEncoded") != null)
                    path = PathUtils.DecodePath(BVModule.GetValue("pathEncoded"));

                if (BVModule.GetValue("rotationVector") != null)
                {
                    switch (BVModule.GetValue("rotationVector"))
                    {
                        case "0":
                            rotationVector = Vector3d.up;
                            break;
                        case "1":
                            rotationVector = Vector3d.down;
                            break;
                        case "2":
                            rotationVector = Vector3d.forward;
                            break;
                        case "3":
                            rotationVector = Vector3d.back;
                            break;
                        case "4":
                            rotationVector = Vector3d.right;
                            break;
                        case "5":
                            rotationVector = Vector3d.left;
                            break;
                        default:
                            rotationVector = Vector3d.back;
                            break;
                    }
                }
                else
                    rotationVector = Vector3d.back;
            }

            State = VesselState.Idle;
            if (shutdown)
                State = VesselState.ControllerDisabled;

            lastTimeUpdated = 0;
            mainStarIndex = 0; // In the most cases The Sun
        }


        /// <summary>
        /// Get controller type
        /// </summary>
        /// <returns></returns>
        internal virtual int GetControllerType()
        {
            return -1;
        }


        #region Main window texts

        /// <summary>
        /// Get vessel state
        /// </summary>
        /// <returns></returns>
        internal VesselState GetVesselState()
        {
            if (vessel.isActiveVessel)
                return VesselState.Current;
            return State;
        }


        /// <summary>
        /// Get textual reprezentation of the vessel status
        /// </summary>
        /// <returns></returns>
        internal string GetVesselStateText()
        {
            if (vessel.isActiveVessel)
                return Localizer.Format("#LOC_BV_Status_Current");
            switch (State)
            {
                case VesselState.Idle:
                    return Localizer.Format("#LOC_BV_Status_Idle");
                case VesselState.ControllerDisabled:
                    return Localizer.Format("#LOC_BV_Status_Disabled");
                case VesselState.AwaitingSunlight:
                    return Localizer.Format("#LOC_BV_Status_AwaitingSunlight");
                case VesselState.Moving:
                    return Localizer.Format("#LOC_BV_Status_Moving");
                default:
                    return Localizer.Format("#LOC_BV_Status_Idle");
            }
        }

        #endregion


        #region Status window texts

        internal virtual List<DisplayedSystemCheckResult> GetDisplayedSystemCheckResults()
        {
            if (displayedSystemCheckResults == null) // Just to be sure
                displayedSystemCheckResults = new List<DisplayedSystemCheckResult>();

            displayedSystemCheckResults.Clear();

            DisplayedSystemCheckResult result = new DisplayedSystemCheckResult
            {
                Toggle = false,
                Label = Localizer.Format("#LOC_BV_Control_TargetLat"),
                Text = targetLatitude.ToString("0.####"),
                Tooltip = ""
            };
            displayedSystemCheckResults.Add(result);

            result = new DisplayedSystemCheckResult
            {
                Toggle = false,
                Label = Localizer.Format("#LOC_BV_Control_TargetLon"),
                Text = targetLongitude.ToString("0.####"),
                Tooltip = ""
            };
            displayedSystemCheckResults.Add(result);

            result = new DisplayedSystemCheckResult
            {
                Toggle = false,
                Label = Localizer.Format("#LOC_BV_Control_Distance"),
                Text = Tools.ConvertDistanceToText(RemainingDistanceToTarget),
                Tooltip = ""
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
        internal virtual bool FindRoute(double lat, double lon)
        {
            return FindRoute(lat, lon, TileTypes.Land | TileTypes.Ocean);
        }


        /// <summary>
        /// Find a route to the target using only specified tile types (route on land, water or both)
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <param name="tileType"></param>
        /// <returns></returns>
        protected bool FindRoute(double targetLat, double targetLon, TileTypes tileType)
        {
            bool result = false;

            PathFinder pathFinder = new PathFinder(vessel.latitude, vessel.longitude, targetLat, targetLon, vessel.mainBody, tileType);
            pathFinder.FindPath();

            double dist = pathFinder.GetDistance();
            if (dist > 0) // Path found
            {
                targetLatitude = targetLat;
                targetLongitude = targetLon;
                distanceToTarget = dist;
                path = PathUtils.HexToWaypoint(pathFinder.path);
                result = true;
            }
            else // Path not found
                result = false;

            return result;
        }

        #endregion


        /// <summary>
        /// Check the systems
        /// </summary>
        internal virtual void SystemCheck()
        {
            mainStarIndex = Tools.GetMainStar(vessel).flightGlobalsIndex;
        }


        /// <summary>
        /// Activate autopilot
        /// </summary>
        internal virtual bool Activate()
        {
            if (distanceToTarget == 0)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BV_Warning_NoRoute", 5f)).color = Color.yellow;
                return false;
            }

            BonVoyageModule module = vessel.FindPartModuleImplementing<BonVoyageModule>();
            if (module != null)
            {
                distanceTravelled = 0;
                lastTimeUpdated = 0;
                active = true;

                module.active = active;
                module.targetLatitude = targetLatitude;
                module.targetLongitude = targetLongitude;
                module.distanceToTarget = distanceToTarget;
                module.distanceTravelled = distanceTravelled;
                module.pathEncoded = PathUtils.EncodePath(path);

                BonVoyage.Instance.AutopilotActivated(true);
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BV_BonVoyage"), 5f);
            }

            return active;
        }


        /// <summary>
        /// Deactivate autopilot
        /// </summary>
        internal virtual bool Deactivate()
        {
            BonVoyageModule module = vessel.FindPartModuleImplementing<BonVoyageModule>();
            if (module != null)
            {
                active = false;
                targetLatitude = 0;
                targetLongitude = 0;
                distanceToTarget = 0;
                distanceTravelled = 0;
                path = null;

                module.active = active;
                module.targetLatitude = targetLatitude;
                module.targetLongitude = targetLongitude;
                module.distanceToTarget = distanceToTarget;
                module.distanceTravelled = distanceTravelled;
                module.pathEncoded = "";

                BonVoyage.Instance.AutopilotActivated(false);
            }

            return !active;
        }


        /// <summary>
        /// Update vessel
        /// </summary>
        /// <param name="currentTime"></param>
        internal virtual void Update(double currentTime)
        {
            if (vessel == null)
                return;
            if (vessel.isActiveVessel)
            {
                if (active)
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BV_AutopilotActive"), 10f).color = Color.red;
                return;
            }

            if (!active || vessel.loaded)
                return;

            State = VesselState.Idle;

            Save(currentTime);
        }


        /// <summary>
        /// Save data to ProtoVessel
        /// </summary>
        protected void Save(double currentTime)
        {
            lastTimeUpdated = currentTime;

            BVModule.SetValue("distanceTravelled", distanceTravelled.ToString());
            BVModule.SetValue("lastTimeUpdated", currentTime.ToString());

            vessel.protoVessel.latitude = vessel.latitude;
            vessel.protoVessel.longitude = vessel.longitude;
            vessel.protoVessel.altitude = vessel.altitude;
            vessel.protoVessel.landedAt = vessel.mainBody.bodyName;
            vessel.protoVessel.displaylandedAt = vessel.mainBody.bodyDisplayName.Replace("^N", "");
        }


        /// <summary>
        /// Check if unmanned vessel has connection
        /// </summary>
        /// <returns></returns>
        internal bool CheckConnection()
        {
            if (vessel.GetCrewCount() == 0) // Unmanned -> check connection
            {
                // CommNet
                if (vessel.Connection.ControlState != CommNet.VesselControlState.ProbeFull)
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BV_Warning_NoConnection", 5f)).color = Color.red;
                    return false;
                }

                // RemoteTech
                if (Tools.AssemblyIsLoaded("RemoteTech"))
                {
                    if (RemoteTechWrapper.IsRemoteTechEnabled() && !RemoteTechWrapper.HasAnyConnection(vessel.id) && !RemoteTechWrapper.HasLocalControl(vessel.id))
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BV_Warning_NoConnection", 5f)).color = Color.red;
                        return false;
                    }
                }
            }
            return true;
        }


        /// <summary>
        /// Deduct used amount from resource tanks
        /// </summary>
        internal void ProcessResources()
        {
            IResourceBroker broker = new ResourceBroker();
            if (fuelCells.Use)
            {
                var iList = fuelCells.InputResources;
                for (int i = 0; i < iList.Count; i++)
                {
                    iList[i].MaximumAmountAvailable -= broker.RequestResource(vessel.rootPart, iList[i].Name, iList[i].CurrentAmountUsed, 1, ResourceFlowMode.ALL_VESSEL);
                    iList[i].CurrentAmountUsed = 0;
                }
            }
        }

    }

}
