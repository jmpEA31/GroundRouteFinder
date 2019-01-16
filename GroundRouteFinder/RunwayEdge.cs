using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder
{
    public class RunwayEdge
    {
        public Vertex V1;
        public Vertex V2;

        public RunwayEdge(Vertex v1, Vertex v2)
        {
            V1 = v1;
            V2 = v2;
        }
    }
}
