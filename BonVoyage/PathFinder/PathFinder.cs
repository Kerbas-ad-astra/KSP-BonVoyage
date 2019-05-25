using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BonVoyage
{
    [Flags]
    internal enum TileTypes
    {
        Land = 0x1,
        Ocean = 0x2
    }

    /// <summary>
    /// Hexagonal grid for pathfinding will be used
    /// https://tbswithunity3d.wordpress.com/2012/02/23/hexagonal-grid-path-finding-using-a-algorithm/
    /// </summary>

    internal class PathFinder
    {
        internal const double StepSize = 1000;

        internal struct Point
        {
            internal int X, Y;

            internal Point(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        private class Tile
        {
            internal int x;
            internal int y;
            internal Hex hex;

            internal Tile(int x, int y, Hex hex)
            {
                this.x = x;
                this.y = y;
                this.hex = hex;
            }
        }

        private Dictionary<int, Point> directions;

        private double startLatitude;
        private double startLongitude;
        private double targetLatitude;
        private double targetLongitude;
        private CelestialBody mainBody;
        TileTypes tileTypes;
        private List<Hex> tiles;
        internal Path<Hex> path;


        Func<Hex, Hex, double> distance = (node1, node2) => StepSize;
        Func<Hex, double> estimate;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="startLat"></param>
        /// <param name="startLon"></param>
        /// <param name="targetLat"></param>
        /// <param name="targetLon"></param>
        /// <param name="body"></param>
        /// <param name="types"></param>
        internal PathFinder(double startLat, double startLon, double targetLat, double targetLon, CelestialBody body, TileTypes types)
        {
            startLatitude = startLat;
            startLongitude = startLon;
            targetLatitude = targetLat;
            targetLongitude = targetLon;
            mainBody = body;
            tileTypes = types;
            estimate = Estimate;

            tiles = new List<Hex>();

            directions = new Dictionary<int, Point>
            {
                { 0, new Point(0, -1) }, // 0 degree
                { 60, new Point(1, -1) }, // 60
                { 120, new Point(1, 0) }, // 120
                { 180, new Point(0, 1) }, // 180
                { 240, new Point(-1, 1) }, // 240
                { 300, new Point(-1, 0) } // 300
            };
        }


        /// <summary>
        /// Find path to the target
        /// </summary>
        internal void FindPath()
        {
            double distanceToTarget = GeoUtils.GetDistance(startLatitude, startLongitude, targetLatitude, targetLongitude, mainBody.Radius);

            if (distanceToTarget < StepSize)
                return;

            double bearing = GeoUtils.InitialBearing(startLatitude, startLongitude, targetLatitude, targetLongitude);
            double altitude = GeoUtils.TerrainHeightAt(startLatitude, startLongitude, mainBody);
            int x = 0;
            int y = 0;
            Hex start = new Hex(startLatitude, startLongitude, altitude, bearing, x, y, this);
            tiles.Add(start);

            double straightPath = 0;
            
            while (straightPath < distanceToTarget)
            {
                GetNeighbours(x, y, false);
                x += directions[0].X;
                y += directions[0].Y;
                straightPath += StepSize;
            }
            Hex destination = tiles.Find(t => (t.X == x + directions[180].X) && (t.Y == y + directions[180].Y));
            
            path = Path<Hex>.FindPath<Hex>(start, destination, distance, estimate);
        }


        private double Estimate(Hex hex)
        {
            return GeoUtils.GetDistance(hex.Latitude, hex.Longitude, targetLatitude, targetLongitude, mainBody.Radius);
        }


        internal IEnumerable<Hex> GetNeighbours(int x, int y, bool passable = true)
        {
            var tile = tiles.Find(t => (t.X == x) && (t.Y == y));
            if (tile == null)
                return null;

            List<Hex> neighbours = new List<Hex>();
            foreach (var direction in directions)
            {
                int dirX = direction.Value.X;
                int dirY = direction.Value.Y;
                var neighbour = tiles.Find(n => (n.X == tile.X + dirX) && (n.Y == tile.Y + dirY));
                if (neighbour == null)
                {
                    double[] coords = GeoUtils.GetLatitudeLongitude(tile.Latitude, tile.Longitude, tile.Bearing + direction.Key, StepSize, mainBody.Radius);
                    double newBearing = GeoUtils.FinalBearing(tile.Latitude, tile.Longitude, coords[0], coords[1]);
                    newBearing = (newBearing - direction.Key + 360) % 360;
                    double altitude = GeoUtils.TerrainHeightAt(coords[0], coords[1], mainBody);
                    neighbour = new Hex(coords[0], coords[1], altitude, newBearing, tile.X + dirX, tile.Y + dirY, this);
                }
                neighbours.Add(neighbour);
                tiles.Add(neighbour);
            }
            if (passable)
            {
                switch (tileTypes)
                {
                    case TileTypes.Land | TileTypes.Ocean:
                        return neighbours.Where(
                            n => 
                            ((n.Altitude - tile.Altitude) < StepSize / 2) &&
                            ((n.Altitude - tile.Altitude) > 0 - StepSize / 2)
                        );

                    case TileTypes.Land:
                        return neighbours.Where(
                            n => (n.Altitude >= 0 || !mainBody.ocean) &&
                            ((n.Altitude - tile.Altitude) < StepSize / 2) &&
                            ((n.Altitude - tile.Altitude) > 0 - StepSize / 2)
                        );

                    case TileTypes.Ocean:
                        return neighbours.Where(
                            n => (n.Altitude <= 0)
                        );

                    default:
                        return neighbours;
                }
            }
            else
                return neighbours;
        }


        internal double GetDistance()
        {
            if (path != null)
            {
                Hex destination = path.LastStep;
                double appendix = GeoUtils.GetDistance(destination.Latitude, destination.Longitude, targetLatitude, targetLongitude, mainBody.Radius);
                return path.TotalCost + appendix;
            }
            else
                return 0;
        }

    }

}