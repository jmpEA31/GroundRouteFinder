using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder
{
    public struct MeasuredVertex
    {
        public Vertex vertex;
        public double distance;
    }

    public class Vertex
    {
        public ulong Id;

        public List<MeasuredVertex> IncommingVertices;

        public double DistanceToTarget;

        public double Latitude;
        public double Longitude;

        public Vertex PathToTarget;

        public bool Done;

        public const double D2R = (Math.PI / 180.0);

        public Vertex(ulong id, double latitude, double longitude)
        {
            Id = id;
            IncommingVertices = new List<MeasuredVertex>();
            DistanceToTarget = float.MaxValue;
            PathToTarget = null;
            Done = false;

            Latitude = latitude * D2R;
            Longitude = longitude * D2R;
        }

        public void AddEdgeFrom(Vertex sourceVertex)
        {
            IncommingVertices.Add(new MeasuredVertex() { vertex = sourceVertex, distance = Distance(sourceVertex.Latitude, sourceVertex.Longitude) });
        }

        public double Distance(double latitudeOther, double longitudeOther)
        {
            double dLongitude = Longitude - longitudeOther;
            double dLatitude = Latitude - latitudeOther;

            double result1 = Math.Pow(Math.Sin(dLatitude / 2.0), 2.0) +
                          Math.Cos(latitudeOther) * Math.Cos(Latitude) *
                          Math.Pow(Math.Sin(dLongitude / 2.0), 2.0);

            // Using 3956 as the number of miles around the earth
            double result2 = /*3956.0 * 2.0 * */
                          Math.Atan2(Math.Sqrt(result1), Math.Sqrt(1.0 - result1));

            return result2;
        }
    }
}
