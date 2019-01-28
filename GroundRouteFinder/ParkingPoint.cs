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
        public bool Inbound;

        public ParkingPoint(double latitude, double longitude, int speed, string name, double bearing, bool inbound) 
            : base(latitude, longitude, speed, name)
        {
            Bearing = bearing;
            Inbound = inbound;
        }

        public override void Write(StreamWriter sw)
        {
            if (Inbound)
                sw.Write($"{Latitude * VortexMath.Rad2Deg:0.00000000} {Longitude * VortexMath.Rad2Deg:0.00000000} {Speed} {Bearing * VortexMath.Rad2Deg:0} 0 0 {Name}\n");
            else
                sw.Write($"{Latitude * VortexMath.Rad2Deg:0.00000000} {Longitude * VortexMath.Rad2Deg:0.00000000} {-Speed} {Bearing * VortexMath.Rad2Deg:0} 0 0 {Name}\n");
        }

        public override void WriteKML(StreamWriter sw)
        {
            sw.WriteLine($"<Placemark><styleUrl>#Parking</styleUrl><name>{Name}</name><Point><coordinates>{Longitude * VortexMath.Rad2Deg},{Latitude * VortexMath.Rad2Deg},0</coordinates></Point></Placemark>");
        }

    }
}
