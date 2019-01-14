using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder
{
    public class StartPoint
    {
        public double Latitude;
        public double Longitude;

        public string Type;
        public string Jets;

        public string Name;

        public Vertex NearestVertex;

        public StartPoint()
        {
            NearestVertex = null;
        }


    }
}
