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
    }

    public class Vertex
    {
        public ulong Id;
        public const int Sizes = 6;

        public List<MeasuredVertex> IncommingVertices;

        public double [] DistanceToTarget;
        public Vertex [] PathToTarget;

        public double Latitude;
        public double Longitude;
        public string LatitudeString;
        public string LongitudeString;

        public bool [] Done;

        public const double D2R = (Math.PI / 180.0);

        public Vertex(ulong id, string latitude, string longitude)
        {
            Id = id;
            IncommingVertices = new List<MeasuredVertex>();

            DistanceToTarget = new double[Sizes];
            PathToTarget = new Vertex[Sizes];
            Done = new bool[Sizes];

            for (int i = 0; i < Sizes; i++)
            {
                DistanceToTarget[i] = float.MaxValue;
                PathToTarget[i] = null;
                Done[i] = false;
            }

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

        public void AddEdgeFrom(Vertex sourceVertex, int maxSize)
        {
            IncommingVertices.Add(new MeasuredVertex() { SourceVertex = sourceVertex, RelativeDistance = 0, MaxSize = maxSize });
        }

        public double CrudeRelativeDistanceEstimate(double latitudeOther, double longitudeOther)
        {
            // Not interested in the actual distance between the points
            // Also assuming that on airport scale lat/lon is linear enough
            double dLongitude = Longitude - longitudeOther;
            double dLatitude = Latitude - latitudeOther;
            return Math.Sqrt(dLongitude * dLongitude + dLatitude * dLatitude);
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
