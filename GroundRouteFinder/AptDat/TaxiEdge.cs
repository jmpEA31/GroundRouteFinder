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
        public List<string> ActiveForRunways;

        public bool IsRunway;
        public XPlaneAircraftCategory MaxCategory;
        public string LinkName;

        public double DistanceKM;
        public double Bearing;

        public TaxiEdge ReverseEdge;

        public TaxiEdge(TaxiNode startNode, TaxiNode endNode, bool isRunway, XPlaneAircraftCategory maxSize, string linkName)
        {
            StartNode = startNode;
            EndNode = endNode;

            IsRunway = isRunway;
            MaxCategory = maxSize;
            LinkName = linkName;

            ActiveForRunways = new List<string>();
            ActiveZone = false;

            ReverseEdge = null;
        }

        public string ActiveForRunway(string preferred)
        {
            if (ActiveForRunways.Count() == 0)
                return preferred;
            else if (ActiveForRunways.Contains(preferred))
                return preferred;
            else
                return ActiveForRunways.FirstOrDefault();
        }

        public void Compute()
        {
            DistanceKM = VortexMath.DistanceKM(StartNode, EndNode);
            Bearing = VortexMath.BearingRadians(StartNode, EndNode);
        }
    }
}
