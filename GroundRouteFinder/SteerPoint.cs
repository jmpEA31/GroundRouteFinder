using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder
{
    public class SteerPoint : LocationObject
    {
        public int Speed;
        public string Name;
        public bool Protected;

        public SteerPoint(double latitude, double longitude, int speed, string name, bool @protected = false)
            : base()
        {
            Latitude = latitude;
            Longitude = longitude;
            Speed = speed;
            Name = name;
            Protected = @protected;
        }

        public virtual SteerPoint Duplicate()
        {
            return new SteerPoint(this.Latitude, this.Longitude, this.Speed, this.Name);
        }

        public virtual void Write(StreamWriter sw)
        {
            sw.Write($"{Latitude * VortexMath.Rad2Deg:0.00000000} {Longitude * VortexMath.Rad2Deg:0.00000000} {Speed} -1 0 0 {Name}\n");
        }
    }
}
