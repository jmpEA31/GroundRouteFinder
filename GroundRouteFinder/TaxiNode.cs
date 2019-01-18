using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder
{
    public class MeasuredNode
    {
        public TaxiNode SourceNode;
        public int MaxSize;
        public double RelativeDistance;
        public double? Bearing;
    }

    public class TaxiNode
    {
        public ulong Id;
        public string Name;
        public const int Sizes = 6;

        public List<MeasuredNode> IncomingVertices;

        public double DistanceToTarget;
        public TaxiNode PathToTarget;

        public double Latitude;
        public double Longitude;
        public string LatitudeString;
        public string LongitudeString;

        public bool IsRunwayEdge;
        public bool IsNonRunwayEdge;

        public double TemporaryDistance;

        public TaxiNode(ulong id, string latitude, string longitude)
        {
            Id = id;
            IncomingVertices = new List<MeasuredNode>();

            IsRunwayEdge = false;
            IsNonRunwayEdge = false;

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
            foreach (MeasuredNode mv in IncomingVertices)
            {
                mv.RelativeDistance = VortexMath.DistancePyth(Latitude, Longitude, mv.SourceNode.Latitude, mv.SourceNode.Longitude);
            }
        }

        public void AddEdgeFrom(TaxiNode sourceVertex, int maxSize, bool isRunway)
        {
            if (isRunway)
                IsRunwayEdge = true;
            else
                IsNonRunwayEdge = true;

            IncomingVertices.Add(new MeasuredNode() { SourceNode = sourceVertex, RelativeDistance = 0, MaxSize = maxSize });
        }
    }
}
