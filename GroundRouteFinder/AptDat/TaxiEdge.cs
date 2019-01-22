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
        public ulong StartNodeId;
        public ulong EndNodeId;
        public bool IsRunway;
        public int MaxSize;
        public string LinkName;

        public TaxiEdge(ulong startNodeId, ulong endNodeId, bool isRunway, int maxSize, string linkName)
        {
            StartNodeId = startNodeId;
            EndNodeId = endNodeId;
            IsRunway = isRunway;
            MaxSize = maxSize;
            LinkName = linkName;

            ActiveFor = "";
            ActiveZone = false;
        }
    }
}
