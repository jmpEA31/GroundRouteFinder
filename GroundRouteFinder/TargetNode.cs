using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder
{
    public class TargetNode
    {
        public double Latitude;
        public double Longitude;

        public TaxiNode NearestVertex;

        public TargetNode()
        {
            NearestVertex = null;
        }
    }
}
