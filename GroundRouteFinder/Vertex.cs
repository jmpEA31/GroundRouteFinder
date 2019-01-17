using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder
{
    public class MeasuredVertex
    {
        public Vertex SourceVertex;
        public int MaxSize;
        public double RelativeDistance;
        public double? Bearing;
    }

    public class Vertex
    {
        public ulong Id;
        public string Name;
        public const int Sizes = 6;

        public List<MeasuredVertex> IncommingVertices;

        public double DistanceToTarget;
        public Vertex PathToTarget;

        public double Latitude;
        public double Longitude;
        public string LatitudeString;
        public string LongitudeString;

        public bool IsRunwayEdge;
        public bool IsNonRunwayEdge;

        public double TemporaryDistance;

        public const double D2R = (Math.PI / 180.0);

        public Vertex(ulong id, string latitude, string longitude)
        {
            Id = id;
            IncommingVertices = new List<MeasuredVertex>();

            IsRunwayEdge = false;
            IsNonRunwayEdge = false;

            DistanceToTarget = double.MaxValue;
            PathToTarget = null;

            LatitudeString = latitude;
            LongitudeString = longitude;
        }

        public void ComputeLonLat()
        {
            Latitude = double.Parse(LatitudeString) * D2R;
            Longitude = double.Parse(LongitudeString) * D2R;
        }

        public void ComputeDistances()
        {
            foreach (MeasuredVertex mv in IncommingVertices)
            {
                mv.RelativeDistance = CrudeRelativeDistanceEstimate(mv.SourceVertex.Latitude, mv.SourceVertex.Longitude);
            }
        }

        public void AddEdgeFrom(Vertex sourceVertex, int maxSize, bool isRunway)
        {
            if (isRunway)
                IsRunwayEdge = true;
            else
                IsNonRunwayEdge = true;

            IncommingVertices.Add(new MeasuredVertex() { SourceVertex = sourceVertex, RelativeDistance = 0, MaxSize = maxSize });
        }

        public double CrudeRelativeDistanceEstimate(double latitudeOther, double longitudeOther)
        {
            return CrudeRelativeDistanceEstimate(Latitude, Longitude, latitudeOther, longitudeOther);
        }

        public static Double CrudeRelativeDistanceEstimate(double φ1, double λ1, double φ2, double λ2)
        {
            // Not interested in the actual distance between the points
            // Also assuming that on airport scale lat/lon is linear enough
            double dλ = λ1 - λ2;
            double dφ = φ1 - φ2;
            return Math.Sqrt(dλ * dλ + dφ * dφ);
        }

        public double RelativeDistance(double latitudeOther, double longitudeOther)
        {
            double dLongitude = Longitude - longitudeOther;
            double dLatitude = Latitude - latitudeOther;

            double result1 = Math.Pow(Math.Sin(dLatitude / 2.0), 2.0) +
                          Math.Cos(latitudeOther) * Math.Cos(Latitude) *
                          Math.Pow(Math.Sin(dLongitude / 2.0), 2.0);

            // No conversion to KM's or miles, just relative
            return Math.Atan2(Math.Sqrt(result1), Math.Sqrt(1.0 - result1));
        }

        public double DistanceKM(double latitudeOther, double longitudeOther)
        {
            double dLongitude = Longitude - longitudeOther;
            double dLatitude = Latitude - latitudeOther;

            double result1 = Math.Pow(Math.Sin(dLatitude / 2.0), 2.0) +
                          Math.Cos(latitudeOther) * Math.Cos(Latitude) *
                          Math.Pow(Math.Sin(dLongitude / 2.0), 2.0);

            return 2.0 * 6371.0 * Math.Atan2(Math.Sqrt(result1), Math.Sqrt(1.0 - result1));
        }

    }
}
