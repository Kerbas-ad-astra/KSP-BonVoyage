using KSP.Localization;
using UnityEngine;

namespace BonVoyage
{
    /// <summary>
    /// Part module of Bon Voyage
    /// </summary>
    class BonVoyageModule : PartModule
    {
        #region KSP Fields

        /// <summary>
        /// BonVoyage controller is active (moving vessel)
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool active = false;

        /// <summary>
        /// BonVoyage controller is shutdown
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool shutdown = false;

        /// <summary>
        /// Vessel arrived at destination
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool arrived = false;

        /// <summary>
        /// Vessel type - 0 - rover, 1 - ship
        /// </summary>
        // localize, when ship part is ready
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Vessel type", category = "Bon Voyage")]
        [UI_ChooseOption(scene = UI_Scene.None, options = new[] { "0", "1" }, display = new[] { "Rover", "Ship" })]
        public string vesselType = "0";

        /// <summary>
        /// Rotation vector
        /// </summary>
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "#LOC_BV_ContextMenu_RV", advancedTweakable = true, category = "Bon Voyage")]
        [UI_ChooseOption(scene = UI_Scene.Flight, options = new[] { "0", "1", "2", "3", "4", "5" }, display = new[] { "#LOC_BV_ContextMenu_RV_Up", "#LOC_BV_ContextMenu_RV_Down", "#LOC_BV_ContextMenu_RV_Forward",
            "#LOC_BV_ContextMenu_RV_Back", "#LOC_BV_ContextMenu_RV_Right", "#LOC_BV_ContextMenu_RV_Left" })]
        public string rotationVector = "3";

        /// <summary>
        /// Target latitude
        /// </summary>
        [KSPField(isPersistant = true)]
        public double targetLatitude = 0;

        /// <summary>
        /// Target longitude
        /// </summary>
        [KSPField(isPersistant = true)]
        public double targetLongitude = 0;

        /// <summary>
        /// Distance to target
        /// </summary>
        [KSPField(isPersistant = true)]
        public double distanceToTarget = 0;

        /// <summary>
        /// Distance travelled
        /// </summary>
        [KSPField(isPersistant = true)]
        public double distanceTravelled = 0;

        /// <summary>
        /// Encoded path
        /// </summary>
        [KSPField(isPersistant = true)]
        public string pathEncoded = "";

        /// <summary>
        /// Average vessel speed
        /// </summary>
        [KSPField(isPersistant = true)]
        public double averageSpeed = 0;

        /// <summary>
        /// Average vessel speed at night
        /// </summary>
        [KSPField(isPersistant = true)]
        public double averageSpeedAtNight = 0;

        /// <summary>
        /// Last time when were fields updated
        /// </summary>
        [KSPField(isPersistant = true)]
        public double lastTimeUpdated = 0;

        /// <summary>
        /// Vessel is manned
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool manned = false;

        /// <summary>
        /// Root part height from the terrain
        /// </summary>
        [KSPField(isPersistant = true)]
        public double vesselHeightFromTerrain = 0;

        #endregion


        /// <summary>
        /// Return info about module
        /// </summary>
        /// <returns>Module info</returns>
        public override string GetInfo()
        {
            return "Bon Voyage Controller";
        }


        /// <summary>
        /// Module start
        /// </summary>
        /// <param name="state">Start state</param>
        public override void OnStart(PartModule.StartState state)
        {
            Events["ToggleBVController"].guiName = (!shutdown ? Localizer.Format("#LOC_BV_ContextMenu_Shutdown") : Localizer.Format("#LOC_BV_ContextMenu_Activate"));
            if (HighLogic.LoadedSceneIsEditor)
            {
                //Fields["vesselType"].guiActive = !shutdown;
            }
            if (HighLogic.LoadedSceneIsFlight)
            {
                //Fields["vesselType"].guiActive = !shutdown;
                Fields["rotationVector"].uiControlFlight.onFieldChanged = RotationVectorChanged;
                Fields["rotationVector"].guiActive = !shutdown;
                Events["BVControlPanel"].guiActive = !shutdown;
            }
        }


        #region KSP Events

        /// <summary>
        /// Shutdown/Activate BV controller
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Shutdown Bon Voyage Controller", category = "Bon Voyage", requireFullControl = true)]
        public void ToggleBVController()
        {
            shutdown = !shutdown;
            Events["ToggleBVController"].guiName = (!shutdown ? Localizer.Format("#LOC_BV_ContextMenu_Shutdown") : Localizer.Format("#LOC_BV_ContextMenu_Activate"));
            if (!HighLogic.LoadedSceneIsEditor)
            {
                //Fields["vesselType"].guiActive = !shutdown;
                Fields["rotationVector"].guiActive = !shutdown;
                Events["BVControlPanel"].guiActive = !shutdown;
                if (shutdown)
                {
                    if (active)
                    {
                        BVController controller = BonVoyage.Instance.GetControllerOfVessel(vessel);
                        if (controller != null)
                            controller.Deactivate();
                    }
                }
                BonVoyage.Instance.SetShutdownState(vessel.id, shutdown);
            }
            else
            {
                //Fields["vesselType"].guiActiveEditor = !shutdown;
            }
        }


        /// <summary>
        /// Show BV control panel
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "#LOC_BV_ContextMenu_Panel", category = "Bon Voyage", requireFullControl = true)]
        public void BVControlPanel()
        {
            BonVoyage.Instance.ToggleControlWindow();
        }


        /// <summary>
        /// Raised when rotationVector field was changed
        /// </summary>
        /// <param name="field"></param>
        /// <param name="oldValue"></param>
        private void RotationVectorChanged(BaseField field, object oldValue)
        {
            BVController controller = BonVoyage.Instance.GetControllerOfVessel(vessel);
            if (controller != null)
            {
                switch (rotationVector)
                {
                    case "0":
                        controller.RotationVector = Vector3d.up;
                        break;
                    case "1":
                        controller.RotationVector = Vector3d.down;
                        break;
                    case "2":
                        controller.RotationVector = Vector3d.forward;
                        break;
                    case "3":
                        controller.RotationVector = Vector3d.back;
                        break;
                    case "4":
                        controller.RotationVector = Vector3d.right;
                        break;
                    case "5":
                        controller.RotationVector = Vector3d.left;
                        break;
                    default:
                        controller.RotationVector = Vector3d.back;
                        break;
                }
            }
        }

        #endregion


        /// <summary>
        /// Pick target in the map mode
        /// </summary>
        void OnGUI()
        {
            if (BonVoyage.Instance.GamePaused && !BonVoyage.Instance.ShowUI)
                return;

            // Pick target in the map mode
            if (BonVoyage.Instance.MapMode)
            {
                if (!MapView.MapIsEnabled)
                {
                    BonVoyage.Instance.MapMode = false;
                    return;
                }

                double[] latLon = Tools.PlaceTargetAtCursor(vessel.mainBody);
                if (latLon[0] != double.MinValue)
                {
                    GUI.Label(
                        new Rect(Input.mousePosition.x + 20, Screen.height - Input.mousePosition.y, 200, 55),
                        Localizer.Format("#LOC_BV_Control_Lat") + ": " + latLon[0].ToString("F") + "\n" +
                        Localizer.Format("#LOC_BV_Control_Lon") + ": " + latLon[1].ToString("F") + "\n" +
                        Localizer.Format("#LOC_BV_Control_Biome") + ": " + ScienceUtil.GetExperimentBiome(vessel.mainBody, latLon[0], latLon[1])
                    );
                    // On left mouse click send coordinates and exit map mode
                    if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
                    {
                        if (BonVoyage.Instance.ControlModel != null)
                        {
                            BonVoyage.Instance.ControlModel.Latitude = latLon[0].ToString();
                            BonVoyage.Instance.ControlModel.Longitude = latLon[1].ToString();
                        }
                        BonVoyage.Instance.MapMode = false;
                        MapView.ExitMapView();
                    }
                }
            }
        }

    }

}
