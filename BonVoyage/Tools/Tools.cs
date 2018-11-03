﻿using System;
using UnityEngine;

namespace BonVoyage
{
    static class Tools
    {
        /// <returns>
		/// The full relative path from the main KSP folder to a given texture resource from this mod.
		/// </returns>
		/// <param name="filename">Name of file located in our plugin folder</param>
		/// <param name="GameDataRelative">True if the KSP/GameData portion of the path is assumed, false if we need to provide the full path</param>
		internal static string TextureFilePath(string filename, bool GameDataRelative = true)
        {
            if (GameDataRelative)
            {
                return string.Format("{0}/Textures/{1}", BonVoyage.Name, filename);
            }
            else
            {
                return string.Format("{0}/Textures/{1}",
                    System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    filename);
            }
        }


        /// <summary>
        /// Convert distance in meters to text (meters or kilometers)
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        internal static string ConvertDistanceToText(double distance)
        {
            string result = "-";
            double n = distance;
            if (n > 0)
            {
                if (n < 1000)
                    result = n.ToString("N0") + " m";
                else
                {
                    n = n / 1000;
                    result = n.ToString("0.##") + " km";
                }
            }
            return result;
        }


        /// <summary>
        /// Returns latitude and longitude of a waypoint or double.MinValue, if waypoint is not valid
        /// </summary>
        /// <param name="v"></param>
        /// <returns>[latitude, longitude]</returns>
        internal static double[] GetCurrentWaypointLatLon(Vessel v)
        {
            NavWaypoint waypoint = NavWaypoint.fetch;
            if ((waypoint == null) || !waypoint.IsActive || (waypoint.Body != v.mainBody))
                return new double[2] { double.MinValue, double.MinValue };
            else
                return new double[2] { waypoint.Latitude, waypoint.Longitude };
        }


        /// <summary>
        /// Returns latitude and longitude of a target or double.MinValue, if target is not valid
        /// </summary>
        /// <param name="v"></param>
        /// <returns>[latitude, longitude]</returns>
        internal static double[] GetCurrentTargetLatLon(Vessel v)
        {
            if (v.targetObject == null)
                return new double[2] { double.MinValue, double.MinValue };

            Vessel target = v.targetObject.GetVessel();
            if ((target == null) || (target.situation != Vessel.Situations.LANDED) || (target.mainBody != v.mainBody))
                return new double[2] { double.MinValue, double.MinValue };
            else
                return new double[2] { target.latitude, target.longitude };
        }


        /// <summary>
        /// Place target at cursor in the map mode. Borrowed Waypoint Manager and changed.
        /// Returns latitude and longitude of a target or double.MinValue, if target is not valid
        /// </summary>
        /// <param name="targetBody"></param>
        /// <returns>[latitude, longitude]</returns>
        internal static double[] PlaceTargetAtCursor(CelestialBody targetBody)
        {
            if (targetBody.pqsController == null)
                return new double[2] { double.MinValue, double.MinValue };

            Ray mouseRay = PlanetariumCamera.Camera.ScreenPointToRay(Input.mousePosition);
            mouseRay.origin = ScaledSpace.ScaledToLocalSpace(mouseRay.origin);
            var bodyToOrigin = mouseRay.origin - targetBody.position;
            double curRadius = targetBody.pqsController.radiusMax;
            double lastRadius = 0;
            int loops = 0;
            while (loops < 50)
            {
                Vector3d relSurfacePosition;
                if (PQS.LineSphereIntersection(bodyToOrigin, mouseRay.direction, curRadius, out relSurfacePosition))
                {
                    var surfacePoint = targetBody.position + relSurfacePosition;
                    double alt = targetBody.pqsController.GetSurfaceHeight(QuaternionD.AngleAxis(targetBody.GetLongitude(surfacePoint), Vector3d.down) * QuaternionD.AngleAxis(targetBody.GetLatitude(surfacePoint), Vector3d.forward) * Vector3d.right);
                    double error = Math.Abs(curRadius - alt);
                    if (error < (targetBody.pqsController.radiusMax - targetBody.pqsController.radiusMin) / 100)
                    {
                        return new double[2] {
                            (targetBody.GetLatitude(surfacePoint) + 360) % 360,
                            (targetBody.GetLongitude(surfacePoint) + 360) % 360
                        };
                    }
                    else
                    {
                        lastRadius = curRadius;
                        curRadius = alt;
                        loops++;
                    }
                }
                else
                {
                    if (loops == 0)
                    {
                        break;
                    }
                    else // Went too low, needs to try higher
                    {
                        curRadius = (lastRadius * 9 + curRadius) / 10;
                        loops++;
                    }
                }
            }
            return new double[2] { double.MinValue, double.MinValue };
        }


        /// <summary>
        /// Return main star for a vessel
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        internal static CelestialBody GetMainStar(Vessel v)
        {
            CelestialBody body = v.mainBody;
            while (body.referenceBody != body) // Last body has reference to itself???
            {
                // We are preparing for Kopernicus
                body = body.referenceBody;
            }
            return body;
        }

    }

}
