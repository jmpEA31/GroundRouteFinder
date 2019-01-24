using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder
{
    public class RunwayPoint : SteerPoint
    {
        public string Operations;

        public bool OnRunway;
        public bool IsExiting;

        public RunwayPoint(double latitude, double longitude, int speed, string name, string operations)
            : base(latitude, longitude, speed, name)
        {
            IsExiting = false;
            OnRunway = false;
            Operations = operations;
        }

        public override SteerPoint Duplicate()
        {
            return new RunwayPoint(this.Latitude, this.Longitude, this.Speed, this.Name, this.Operations);
        }

        public override void Write(StreamWriter sw)
        {
            if (IsExiting)
                base.Write(sw);
            else if (OnRunway)
                sw.Write($"{Latitude * VortexMath.Rad2Deg:0.00000000} {Longitude * VortexMath.Rad2Deg:0.00000000} {Speed} -1 {Operations} 2\n");
            else
                sw.Write($"{Latitude * VortexMath.Rad2Deg:0.00000000} {Longitude * VortexMath.Rad2Deg:0.00000000} {Speed} -1 {Operations} 1\n");
        }
    }
}
