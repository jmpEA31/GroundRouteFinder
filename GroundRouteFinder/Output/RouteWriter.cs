﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder.Output
{
    public abstract class RouteWriter : StreamWriter
    {
        public static RouteWriter Create(int type, string path, string allSizes = "", int cargo = 0, int military = 0, string designator = "", string parkingCenter = "")
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

        public RouteWriter(string path)
            : base(path, false, Encoding.UTF8)
        {
        }

        public abstract void Write(SteerPoint steerPoint);
    }
}
