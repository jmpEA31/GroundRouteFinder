using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder
{
    public class ParkingPoint : SteerPoint
    {
        public double Bearing;

        public ParkingPoint(double latitude, double longitude, int speed, string name, double bearing) 
            : base(latitude, longitude, speed, name)
        {
            Bearing = bearing;
        }

        public override void Write(StreamWriter sw)
        {
            sw.Write($"{Latitude * VortexMath.Rad2Deg:0.00000000} {Longitude * VortexMath.Rad2Deg:0.00000000} {Speed} {Bearing * VortexMath.Rad2Deg:0} 0 0 {Name}\n");
        }
    }
}
