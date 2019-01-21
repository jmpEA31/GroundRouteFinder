using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder.AptDat
{
   public class RunwayExitSpot
    {
        public TaxiNode ExitNode;
        public List<TaxiNode> ExitPoints;

        public RunwayExitSpot()
        {
            ExitPoints = new List<TaxiNode>();
        }
    }
}
