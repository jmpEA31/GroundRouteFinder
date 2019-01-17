using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder
{
    public class TargetNode
    {
        public double ActualLatitude;
        public double ActualLongitude;

        public Vertex NearestVertex;

        public TargetNode()
        {
            NearestVertex = null;
        }
    }
}
