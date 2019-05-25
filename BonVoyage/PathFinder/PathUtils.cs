using System;
using System.Collections.Generic;

namespace BonVoyage
{
    /// <summary>
    /// Pathfinder helper utils
    /// </summary>
    internal class PathUtils
    {
        /// <summary>
        /// Path waypoint
        /// </summary>
        internal struct WayPoint
        {
            internal double latitude;
            internal double longitude;
            internal WayPoint(double lat, double lon)
            {
                latitude = lat;
                longitude = lon;
            }
        }


        /// <summary>
        /// Convert Path<Hex> to List<Waypoint>
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static List<WayPoint> HexToWaypoint(Path<Hex> path)
        {
            List<WayPoint> result = new List<WayPoint>();
            foreach (Hex point in path)
                result.Add(new WayPoint(point.Latitude, point.Longitude));
            result.Reverse();
            return result;
        }


        /// <summary>
        /// Encode path to base64 string
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static string EncodePath(List<WayPoint> path)
        {
            if (path == null)
                return "";

            string result = "";
            for (int i = 0; i < path.Count; i++)
                result += path[i].latitude.ToString("R") + ":" + path[i].longitude.ToString("R") + ";";

            // Replace forward slash with # (two forward slashes seems to be interpreted as a start of the comment when read from a save file)
            var textBytes = System.Text.Encoding.UTF8.GetBytes(result);
            return Convert.ToBase64String(textBytes).Replace('/', '#');
        }


        /// <summary>
        /// Decode path from base64 encoded string
        /// </summary>
        /// <param name="pathEncoded"></param>
        /// <returns></returns>
        internal static List<WayPoint> DecodePath(string pathEncoded)
        {
            if (pathEncoded == null || pathEncoded.Length == 0)
                return null;

            // Replace # with forward slash (two forward slashes seems to be interpreted as a start of the comment when read from a save file)
            var encodedBytes = Convert.FromBase64String(pathEncoded.Replace('#', '/'));
            pathEncoded = System.Text.Encoding.UTF8.GetString(encodedBytes);

            List<WayPoint> result = new List<WayPoint>();
            string[] wps = pathEncoded.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < wps.Length; i++)
            {
                string[] latlon = wps[i].Split(':');
                result.Add(new WayPoint(double.Parse(latlon[0]), double.Parse(latlon[1])));
            }
            return result;
        }

    }

}
