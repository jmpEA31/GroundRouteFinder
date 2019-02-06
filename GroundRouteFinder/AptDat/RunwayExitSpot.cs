using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder.AptDat
{
   public class RunwayTakeOffSpot
    {
        public TaxiNode TakeOffNode;
        public List<TaxiNode> EntryPoints;
        public double TakeOffLengthRemaining;

        public RunwayTakeOffSpot()
        {
            EntryPoints = new List<TaxiNode>();
        }
    }
}
