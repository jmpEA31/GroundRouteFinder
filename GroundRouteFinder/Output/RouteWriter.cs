using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder.Output
{
    public abstract class RouteWriter : InvariantWriter
    {
        public static RouteWriter Create(int type, string path, string allSizes = "", int cargo = -1, int military = -1, string designator = "", string parkingCenter = "")
        {
            switch (type)
            {
                case 0:
                    return new KmlWriter(path);
                case 1:
                default:
                    return new TxtWriter(path, allSizes, cargo, military, designator, parkingCenter);
            }
        }

        /// <summary>
        /// Create a routewriter
        /// </summary>
        /// <param name="path"></param>
        public RouteWriter(string path, Encoding encoding)
            : base(path, encoding)
        {
        }

        public abstract void Write(SteerPoint steerPoint);
    }
}
