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
        public double BearingToTarget;
        public string NameToTarget;
        public bool PathIsRunway;

        public string LatitudeString;
        public string LongitudeString;

        // Both will be true if there are runway edges and taxiway edges coming into this node
        // todo: figure out a cleaner way to deal with this. EDges can be rwy or twy, but also may (not) have
        // runway operations assigned to them. It's probably best to not look at the runwayness of nodes, just edges
        public bool IsRunwayNode;
        public bool IsNonRunwayNode;

        public double TemporaryDistance;

        public TaxiNode(ulong id, string latitude, string longitude)
            : base()
        {
            Id = id;
            IncomingNodes = new List<TaxiEdge>();

            IsRunwayNode = false;
            IsNonRunwayNode = false;

            DistanceToTarget = double.MaxValue;
            NextNodeToTarget = null;

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
            if (edge.IsRunway)
                IsRunwayNode = true;
            else
                IsNonRunwayNode = true;

            IncomingNodes.Add(edge);
        }
    }
}
