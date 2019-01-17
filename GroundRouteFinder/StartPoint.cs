using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder
{
    public class StartPoint : TargetNode
    {
        public string Type;
        public string Jets;
        public string Name;

        public double Bearing;

        public StartPoint() 
            : base()
        {
            NearestVertex = null;
        }

        public void FindTaxiOutPoint()
        {
            Vertex first = NearestVertex;
            Vertex second = first.PathToTarget;

            StreamWriter sw = File.CreateText("D:\\hick.csv");
            sw.WriteLine("lat,lon,title");
            sw.WriteLine($"{first.Latitude * 180.0 / Math.PI},{first.Longitude * 180.0 / Math.PI},first");
            sw.WriteLine($"{second.Latitude * 180.0 / Math.PI},{second.Longitude * 180.0 / Math.PI},second");
            sw.WriteLine($"{ActualLatitude * 180.0 / Math.PI},{ActualLongitude * 180.0 / Math.PI},parking");
          

            double departureBearing = ComputeBearing(first.Latitude, first.Longitude, second.Latitude, second.Longitude);

            double latFirstTarget = 0;
            double lonFirstTarget = 0;

            if (Intersection(first.Latitude, first.Longitude, departureBearing, ActualLatitude, ActualLongitude, (Bearing + Math.PI) % (Math.PI * 2), ref latFirstTarget, ref lonFirstTarget))
            {
                sw.WriteLine($"{latFirstTarget * 180.0 / Math.PI},{lonFirstTarget * 180.0 / Math.PI},push");
            }
            else if (Intersection(first.Latitude, first.Longitude, departureBearing, ActualLatitude, ActualLongitude, Bearing, ref latFirstTarget, ref lonFirstTarget))
            {
                latFirstTarget = -latFirstTarget;
                lonFirstTarget += Math.PI;
                sw.WriteLine($"{latFirstTarget * 180.0 / Math.PI},{lonFirstTarget * 180.0 / Math.PI},push");
            }

            sw.Close();
        }

        public static double ComputeBearing(double lat1, double lon1, double lat2, double lon2)
        {
            double dLon = (lon2 - lon1);
            double dPhi = Math.Log(Math.Tan(lat2 / 2 + Math.PI / 4) / Math.Tan(lat1 / 2 + Math.PI / 4));
            if (Math.Abs(dLon) > Math.PI)
            {
                dLon = dLon > 0 ? -(2 * Math.PI - dLon) : (2 * Math.PI + dLon);
            }
            return Math.Atan2(dLon, dPhi);
        }

        public static bool Intersection(double φ1, double λ1, double θ13, double φ2, double λ2, double θ23, ref double latIntersection, ref double lonIntersection)
        {
            double Δφ = φ2 - φ1;
            double Δλ = λ2 - λ1;

            double sinhalfΔφ = Math.Sin(Δφ / 2);
            double sinhalfΔλ = Math.Sin(Δλ / 2);
            double sinφ1 = Math.Sin(φ1);
            double sinφ2 = Math.Sin(φ2);
            double cosφ1 = Math.Cos(φ1);
            double cosφ2 = Math.Cos(φ2);

            // angular distance p1-p2
            double δ12 = 2 * Math.Asin(Math.Sqrt(sinhalfΔφ * sinhalfΔφ + cosφ1 * Math.Cos(φ2) * sinhalfΔλ * sinhalfΔλ));
            if (δ12 == 0) return false;

            double sinδ12 = Math.Sin(δ12);
            double cosδ12 = Math.Cos(δ12);


            // initial/final bearings between points
            double θa = Math.Acos((sinφ2 - sinφ1 * cosδ12) / (sinδ12 * cosφ1));
            if (double.IsNaN(θa)) θa = 0; // protect against rounding
            double θb = Math.Acos((sinφ1 - sinφ2 * cosδ12) / (sinδ12 * cosφ2));

            double θ12 = Math.Sin(λ2 - λ1) > 0 ? θa : 2 * Math.PI - θa;
            double θ21 = Math.Sin(λ2 - λ1) > 0 ? 2 * Math.PI - θb : θb;

            double α1 = θ13 - θ12; // angle 2-1-3
            double α2 = θ21 - θ23; // angle 1-2-3

            double sinα1 = Math.Sin(α1);
            double sinα2 = Math.Sin(α2);
            double cosα1 = Math.Cos(α1);
            if (sinα1 == 0 && sinα2 == 0) return false; // infinite intersections
            if (sinα1 * sinα2 < 0) return false;      // ambiguous intersection

            double α3 = Math.Acos(-cosα1 * Math.Cos(α2) + sinα1 * sinα2 * cosδ12);
            double δ13 = Math.Atan2(sinδ12 * sinα1 * sinα2, Math.Cos(α2) + cosα1 * Math.Cos(α3));
            latIntersection = Math.Asin(sinφ1 * Math.Cos(δ13) + cosφ1 * Math.Sin(δ13) * Math.Cos(θ13));
            double Δλ13 = Math.Atan2(Math.Sin(θ13) * Math.Sin(δ13) * cosφ1, Math.Cos(δ13) - sinφ1 * Math.Sin(latIntersection));
            lonIntersection = (((λ1 + Δλ13) + 3.0 * Math.PI) % (2.0 * Math.PI)) - Math.PI;

            //return new LatLon(φ3.toDegrees(), (λ3.toDegrees() + 540) % 360 - 180); // normalise to −180..+180°

            //double resLat = latIntersection * 180.0 / Math.PI;
            //double resLon = ((lonIntersection * 180.0 / Math.PI) + 540) % 360 - 180;

            return true;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
