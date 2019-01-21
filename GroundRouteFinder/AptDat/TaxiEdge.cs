using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder.AptDat
{
    public class TaxiEdge
    {
        public bool ActiveZone;
        public string ActiveFor;
        public ulong Node1;
        public ulong Node2;

        public TaxiEdge()
        {
        }
    }
}
