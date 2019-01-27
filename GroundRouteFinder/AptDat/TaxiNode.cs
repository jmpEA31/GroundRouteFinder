using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder.AptDat
{
    public class TaxiNode : LocationObject
    {
        public ulong Id;
        public string Name;
        public const int Sizes = 6;

        public List<TaxiEdge> IncomingNodes;

        public double DistanceToTarget;
        public TaxiNode NextNodeToTarget;
        public TaxiNode OverrideToTarget;
        public double BearingToTarget;
        public string NameToTarget;
        public bool PathIsRunway;

        public string LatitudeString;
        public string LongitudeString;

        public double TemporaryDistance;

        public TaxiNode(ulong id, string latitude, string longitude)
            : base()
        {
            Id = id;
            IncomingNodes = new List<TaxiEdge>();

            DistanceToTarget = double.MaxValue;
            NextNodeToTarget = null;
            OverrideToTarget = null;

            LatitudeString = latitude;
            LongitudeString = longitude;
        }

        public void ComputeLonLat()
        {
            Latitude = double.Parse(LatitudeString) * VortexMath.Deg2Rad;
            Longitude = double.Parse(LongitudeString) * VortexMath.Deg2Rad;
        }

        public void AddEdgeFrom(TaxiEdge edge)
        {
            IncomingNodes.Add(edge);
        }

        public override string ToString()
        {
            return $"{Id} {Name}";
        }
    }
}
