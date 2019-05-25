using System.Collections.Generic;

namespace BonVoyage
{
    internal class Hex : IHasNeighbours<Hex>
    {
        private double latitude;
        internal double Latitude { get { return latitude; } }

        private double longitude;
        internal double Longitude { get { return longitude; } }

        internal double altitude;
        internal double Altitude { get { return altitude; } }

        private double bearing;
        internal double Bearing { get { return bearing; } }

        private int x;
        internal int X { get { return x; } }

        private int y;
        internal int Y { get { return y; } }

        private PathFinder parent;

        internal Hex(double latitude, double longitude, double altitude, double bearing, int x, int y, PathFinder parent)
        {
            this.latitude = latitude;
            this.longitude = longitude;
            this.altitude = altitude;
            this.bearing = bearing;
            this.x = x;
            this.y = y;
            this.parent = parent;
        }

        public IEnumerable<Hex> Neighbours
        {
            get
            {
                return parent.GetNeighbours(this.x, this.y);
            }
        }

    }

}