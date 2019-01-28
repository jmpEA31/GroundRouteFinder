using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder
{
    public class PushbackPoint : SteerPoint
    {
        public PushbackPoint(double latitude, double longitude, int speed, string name)
            : base(latitude, longitude, speed, name, true)
        {
        }

        public override void Write(StreamWriter sw)
        {
            sw.Write($"{Latitude * VortexMath.Rad2Deg:0.00000000} {Longitude * VortexMath.Rad2Deg:0.00000000} {-Speed} -1 0 0 {Name} pushback\n");
        }

        override public void WriteKML(StreamWriter sw)
        {
            sw.WriteLine($"<Placemark><styleUrl>#Pushback</styleUrl><name>{Name}</name><Point><coordinates>{Longitude * VortexMath.Rad2Deg},{Latitude * VortexMath.Rad2Deg},0</coordinates></Point></Placemark>");
        }

    }
}
