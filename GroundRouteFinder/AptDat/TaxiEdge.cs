using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder.AptDat
{
    public class TaxiEdge
    {
        public TaxiNode StartNode;
        public TaxiNode EndNode;

        public bool ActiveZone;
        public string ActiveFor;

        public bool IsRunway;
        public int MaxSize;
        public string LinkName;

        public double DistanceKM;
        public double Bearing;

        public TaxiEdge(TaxiNode startNode, TaxiNode endNode, bool isRunway, int maxSize, string linkName)
        {
            StartNode = startNode;
            EndNode = endNode;

            IsRunway = isRunway;
            MaxSize = maxSize;
            LinkName = linkName;

            ActiveFor = "";
            ActiveZone = false;
        }

        public void Compute()
        {
            DistanceKM = VortexMath.DistanceKM(StartNode, EndNode);
            Bearing = VortexMath.BearingRadians(StartNode, EndNode);
        }
    }
}
