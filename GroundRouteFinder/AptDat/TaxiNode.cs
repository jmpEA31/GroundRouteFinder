using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder.AptDat
{
    public class MeasuredNode
    {
        public string LinkName;
        public bool IsRunway;
        public TaxiNode SourceNode;
        public int MaxSize;
        public double RelativeDistance;
        public double Bearing;
    }

    public class TaxiNode
    {
        public ulong Id;
        public string Name;
        public const int Sizes = 6;

        public List<MeasuredNode> IncomingNodes;

        public double DistanceToTarget;
        public TaxiNode PathToTarget;
        public string NameToTarget;
        public bool PathIsRunway;

        public double Latitude;
        public double Longitude;
        public string LatitudeString;
        public string LongitudeString;

        // Both will be true if there are runway edges and taxiway edges coming into this node
        // todo: figure out a cleaner way to deal with this. EDges can be rwy or twy, but also may (not) have
        // runway operations assigned to them. It's probably best to not look at the runwayness of nodes, just edges
        public bool IsRunwayNode;
        public bool IsNonRunwayNode;

        public double TemporaryDistance;

        public TaxiNode(ulong id, string latitude, string longitude)
        {
            Id = id;
            IncomingNodes = new List<MeasuredNode>();

            IsRunwayNode = false;
            IsNonRunwayNode = false;

            DistanceToTarget = double.MaxValue;
            PathToTarget = null;

            LatitudeString = latitude;
            LongitudeString = longitude;
        }

        public void ComputeLonLat()
        {
            Latitude = double.Parse(LatitudeString) * VortexMath.Deg2Rad;
            Longitude = double.Parse(LongitudeString) * VortexMath.Deg2Rad;
        }

        public void ComputeDistances()
        {
            foreach (MeasuredNode mv in IncomingNodes)
            {
                mv.RelativeDistance = VortexMath.DistancePyth(Latitude, Longitude, mv.SourceNode.Latitude, mv.SourceNode.Longitude);
                mv.Bearing = VortexMath.BearingRadians(mv.SourceNode.Latitude, mv.SourceNode.Longitude, Latitude, Longitude);
            }
        }

        public void AddEdgeFrom(TaxiNode sourceVertex, int maxSize, bool isRunway, string linkName)
        {
            if (isRunway)
                IsRunwayNode = true;
            else
                IsNonRunwayNode = true;

            IncomingNodes.Add(new MeasuredNode() { SourceNode = sourceVertex, RelativeDistance = 0, MaxSize = maxSize, LinkName = linkName, IsRunway = isRunway });
        }
    }
}
