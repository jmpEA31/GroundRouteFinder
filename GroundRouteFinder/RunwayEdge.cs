using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder
{
    public class RunwayEdge
    {
        public TaxiNode V1;
        public TaxiNode V2;

        public RunwayEdge(TaxiNode v1, TaxiNode v2)
        {
            V1 = v1;
            V2 = v2;
        }
    }
}
