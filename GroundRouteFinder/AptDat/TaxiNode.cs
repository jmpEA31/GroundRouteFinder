using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder.AptDat
{
    public class TaxiNode : LocationObject
    {
        public uint Id;
        public string Name;
        public const int Sizes = 6;

        public List<TaxiEdge> IncomingEdges;

        public double DistanceToTarget;
        public TaxiNode NextNodeToTarget;
        public TaxiNode OverrideToTarget;
        public double BearingToTarget;
        public string NameToTarget;
        public bool PathIsRunway;

        public string LatitudeString;
        public string LongitudeString;

        public double TemporaryDistance;

        public TaxiNode(uint id, string latitude, string longitude)
            : base()
        {
            Id = id;
            IncomingEdges = new List<TaxiEdge>();

            DistanceToTarget = double.MaxValue;
            NextNodeToTarget = null;
            OverrideToTarget = null;

            LatitudeString = latitude;
            LongitudeString = longitude;
        }

        public void ComputeLonLat()
        {
            Latitude = VortexMath.ParseDegreesToRadians(LatitudeString);
            Longitude = VortexMath.ParseDegreesToRadians(LongitudeString);
        }

        public void AddEdgeFrom(TaxiEdge edge)
        {
            IncomingEdges.Add(edge);
        }

        public override string ToString()
        {
            return $"{Id} {Name}";
        }
    }
}
