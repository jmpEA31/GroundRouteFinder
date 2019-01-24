using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder
{
    public class LocationObject
    {
        public double Latitude;
        public double Longitude;

        public LocationObject()
        {
            Latitude = 0;
            Longitude = 0;
        }

        public LocationObject(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}
