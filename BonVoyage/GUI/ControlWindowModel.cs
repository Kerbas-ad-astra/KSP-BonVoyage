using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace BonVoyage
{
    /// <summary>
    /// Control window - model part
    /// </summary>
    internal class ControlWindowModel
    {
        // Displayed stats list
        private DialogGUIVerticalLayout statsListLayout = null;


        private double latitude = 0f;
        internal string Latitude
        {
            get { return latitude.ToString(); }
            set
            {
                if ((value.Length == 0) || (value == "."))
                    latitude = 0f;
                else
                    latitude = Convert.ToDouble(value);
            }
        }

        private double longitude = 0f;
        internal string Longitude
        {
            get { return longitude.ToString(); }
            set
            {
                if ((value.Length == 0) || (value == "."))
                    longitude = 0f;
                else
                    longitude = Convert.ToDouble(value);
            }
        }

        private bool controllerActive; // Is controller active and doing it's behind the scenes magic?
        private BVController currentController;


        /// <summary>
        /// Constructor
        /// </summary>
        internal ControlWindowModel()
        {
            // Load from configuration
            CommonWindowProperties.ControlWindowPosition = Configuration.ControlWindowPosition;

            controllerActive = false;
            currentController = null;
        }


        /// <summary>
        /// Set current controller
        /// </summary>
        /// <param name="c"></param>
        internal void SetController(BVController controller)
        {
            currentController = controller;
            if (controller != null)
            {
                if (currentController.CheckConnection())
                    controller.SystemCheck();
                controllerActive = controller.Active;
            }
        }


        /// <summary>
        /// If controller is active, some buttons will be disabled
        /// </summary>
        /// <returns></returns>
        internal bool EnableButtons()
        {
            return !controllerActive;
        }


        /// <summary>
        /// Add control lock/unlock listeners to a text field
        /// </summary>
        /// <param name="text"></param>
        private void TMPFieldOnSelect(string text)
        {
            InputLockManager.SetControlLock(ControlTypes.KEYBOARDINPUT | ControlTypes.UI, "BonVoyageInputFieldLock");
        }
        private void TMPFieldOnDeselect(string text)
        {
            InputLockManager.RemoveControlLock("BonVoyageInputFieldLock");
        }
        internal void AddLockControlToTextField(DialogGUITextInput field)
        {
            field.OnUpdate = () => {
                if (field.uiItem != null)
                {
                    field.OnUpdate = () => { };
                    TMP_InputField TMPField = field.uiItem.GetComponent<TMP_InputField>();
                    TMPField.onSelect.AddListener(TMPFieldOnSelect);
                    TMPField.onDeselect.AddListener(TMPFieldOnDeselect);
                }
            };
        }


        /// <summary>
        /// Return text of the control button
        /// </summary>
        /// <returns></returns>
        internal string GetGoButtonText()
        {
            if (!controllerActive)
                return Localizer.Format("#LOC_BV_Control_Go");
            else
                return Localizer.Format("#LOC_BV_Control_Deactivate");
        }


        /// <summary>
        /// Go button was clicked
        /// </summary>
        internal void GoButtonClicked()
        {
            if (currentController != null)
            {
                if (!currentController.CheckConnection())
                    return;

                if (!controllerActive)
                {
                    controllerActive = currentController.Activate();
                    if (!controllerActive) // Refresh after uncomplete activation - show results of a system check
                        RefreshStatsListLayout();
                }
                else
                {
                    controllerActive = currentController.Deactivate();
                    BonVoyage.Instance.ResetWindows();
                }
            }
            else
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BV_Warning_ControllerNotValid", 5f)).color = Color.yellow;
        }


        /// <summary>
        /// System check button was clicked
        /// </summary>
        internal void SystemCheckButtonClicked()
        {
            if (currentController != null)
            {
                if (currentController.CheckConnection())
                    currentController.SystemCheck();
                RefreshStatsListLayout();
            }
            else
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BV_Warning_ControllerNotValid", 5f)).color = Color.yellow;
        }


        /// <summary>
        ///  Return saved latitude
        /// </summary>
        /// <returns></returns>
        internal string GetLatitude()
        {
            return Latitude;
        }


        /// <summary>
        /// Return saved longitude
        /// </summary>
        /// <returns></returns>
        internal string GetLongitude()
        {
            return Longitude;
        }


        /// <summary>
        /// Set button was clicked
        /// </summary>
        internal void SetButtonClicked()
        {
            if (currentController != null)
            {
                if (currentController.vessel.situation == Vessel.Situations.LANDED)
                {
                    if (!currentController.CheckConnection())
                        return;

                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BV_FindingRoute", 5f));
                    if (currentController.FindRoute(latitude, longitude))
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BV_RouteFound", 5f));
                        RefreshStatsListLayout();
                    }
                    else
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BV_RouteNotFound", 5f));
                }
                else
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BV_Warning_Landed"), 5f).color = Color.yellow;
            }
            else
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BV_Warning_ControllerNotValid", 5f)).color = Color.yellow;
        }


        /// <summary>
        /// Pick on map button was clicked
        /// </summary>
        internal void PickOnMapButtonClicked()
        {
            if (currentController != null)
            {
                if (currentController.vessel.situation == Vessel.Situations.LANDED)
                {
                    MapView.EnterMapView();
                    BonVoyage.Instance.MapMode = true;
                }
                else
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BV_Warning_Landed", 5f)).color = Color.yellow;
            }
            else
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BV_Warning_ControllerNotValid", 5f)).color = Color.yellow;
        }


        /// <summary>
        /// Current target button was clicked
        /// </summary>
        internal void CurrentTargetButtonClicked()
        {
            if (currentController != null)
            {
                double[] cooordinates = Tools.GetCurrentTargetLatLon(currentController.vessel);
                if (cooordinates[0] != double.MinValue)
                {
                    latitude = (cooordinates[0] + 360) % 360;
                    longitude = (cooordinates[1] + 360) % 360;
                }
                else
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BV_Warning_TargetNotValid", 5f)).color = Color.yellow;
            }
            else
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BV_Warning_ControllerNotValid", 5f)).color = Color.yellow;
        }


        /// <summary>
        /// Current waypoint button was clicked
        /// </summary>
        internal void CurrentWaypointButtonClicked()
        {
            if (currentController != null)
            {
                double[] cooordinates = Tools.GetCurrentWaypointLatLon(currentController.vessel);
                if (cooordinates[0] != double.MinValue)
                {
                    latitude = (cooordinates[0] + 360) % 360;
                    longitude = (cooordinates[1] + 360) % 360;
                }
                else
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BV_Warning_WaypointNotValid", 5f)).color = Color.yellow;
            }
            else
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BV_Warning_ControllerNotValid", 5f)).color = Color.yellow;
        }


        /// <summary>
        /// Create table row for displayed result
        /// </summary>
        /// <param name="result"></param>
        /// <returns>DialogGUIHorizontalLayout row</returns>
        private DialogGUIHorizontalLayout CreateListLayoutRow(DisplayedSystemCheckResult result)
        {
            DialogGUIHorizontalLayout row = null;

            if (result.Toggle)
            {
                row = new DialogGUIHorizontalLayout(
                    (result.Tooltip.Length > 0)
                    ?
                    TooltipExtension.DeferTooltip(new DialogGUIToggle(result.GetToggleValue, result.Text, result.ToggleSelectedCallback) { tooltipText = result.Tooltip })
                    :
                    new DialogGUIToggle(result.GetToggleValue, result.Text, result.ToggleSelectedCallback)
                );
            }
            else
            {
                if (result.Tooltip.Length > 0)
                {
                    row = new DialogGUIHorizontalLayout(
                        new DialogGUILabel(result.Label + ":", 100f),
                        new DialogGUILabel(result.Text),
                        new DialogGUISpace(1f),
                        // Add a button with transparent background and label style just to display a tooltip when hovering over it
                        // Transparent sprite is needed to hide button borders
                        TooltipExtension.DeferTooltip(new DialogGUIButton(CommonWindowProperties.transparent, "(?)", () => { }, 17f, 18f, false) { tooltipText = result.Tooltip, guiStyle = CommonWindowProperties.Style_Button_Label })
                    );
                }
                else
                {
                    row = new DialogGUIHorizontalLayout(
                        new DialogGUILabel(result.Label + ":", 100f),
                        new DialogGUILabel(result.Text)
                    );
                }
            }

            return row;
        }


        /// <summary>
        /// Get layout of the list of stats
        /// </summary>
        /// <returns></returns>
        internal DialogGUIVerticalLayout GetStatsListLayout()
        {
            if (currentController != null)
            {
                List<DisplayedSystemCheckResult> resultsList = currentController.GetDisplayedSystemCheckResults();

                DialogGUIBase[] list = new DialogGUIBase[1 + resultsList.Count];
                int index = 0;
                for (int i = 0; i < resultsList.Count; i++)
                {
                    list[index] = CreateListLayoutRow(resultsList[i]);
                    index++;
                }
                list[index] = new DialogGUISpace(3f);
                statsListLayout = new DialogGUIVerticalLayout(list);
            }
            else
            {
                statsListLayout = new DialogGUIVerticalLayout(new DialogGUISpace(3f));
            }
            return statsListLayout;
        }


        /// <summary>
        /// Clear layout of the list of stats
        /// </summary>
        internal void ClearStatsListLayout()
        {
            statsListLayout = null;
        }


        /// <summary>
        /// Refresh list of stats without
        /// </summary>
        internal void RefreshStatsListLayout()
        {
            Stack<Transform> stack = new Stack<Transform>();  // some data on hierarchy of GUI components
            stack.Push(statsListLayout.uiItem.gameObject.transform); // need the reference point of the parent GUI component for position and size

            List<DialogGUIBase> rows = statsListLayout.children;

            // Clear list
            while (rows.Count > 0)
            {
                DialogGUIBase child = rows.ElementAt(0); // Get child
                rows.RemoveAt(0); // Drop row
                child.uiItem.gameObject.DestroyGameObjectImmediate(); // Free memory up
            }

            // Add rows
            if (currentController != null)
            {
                List<DisplayedSystemCheckResult> resultsList = currentController.GetDisplayedSystemCheckResults();

                for (int i = 0; i < resultsList.Count; i++)
                {
                    rows.Add(CreateListLayoutRow(resultsList[i]));
                    rows.Last().Create(ref stack, CommonWindowProperties.ActiveSkin); // required to force the GUI creatio﻿n
                }
            }
            rows.Add(new DialogGUISpace(3f));
            rows.Last().Create(ref stack, CommonWindowProperties.ActiveSkin); // required to force the GUI creatio﻿n
        }

    }

}
