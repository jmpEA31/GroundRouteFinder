using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder
{
    public class VortexMath
    {
        public const double PI = Math.PI;
        public const double PI2 = 2.0 * Math.PI;
        public const double PI3 = 3.0 * Math.PI;
        public const double PI05 = 0.5 * Math.PI;
        public const double PI025 = 0.25 * Math.PI;
        public const double Deg2Rad = Math.PI / 180.0;
        public const double Rad2Deg = 180.0 / Math.PI;

        /// <summary>
        /// Return the distance between two lat/lon points in radians
        /// </summary>
        /// <param name="φ1">Latitude of the first point in radians</param>
        /// <param name="λ1">Longitude of the first point in radians</param>
        /// <param name="φ2">Latitude of the second point in radians</param>
        /// <param name="λ2">Latitude of the second point in radians</param>
        /// <returns>The distance between the two points along a great circle in radians</returns>
        public static double DistanceRadians(double φ1, double λ1, double φ2, double λ2)
        {
            double dλ = λ1 - λ2;
            double dφ = φ1 - φ2;

            double result1 = Math.Pow(Math.Sin(dφ / 2.0), 2.0) + Math.Cos(φ2) * Math.Cos(φ1) * Math.Pow(Math.Sin(dλ / 2.0), 2.0);

            return 2.0 * Math.Atan2(Math.Sqrt(result1), Math.Sqrt(1.0 - result1));
        }


        /// <summary>
        /// Return the distance between two lat/lon points in KM's
        /// </summary>
        /// <param name="φ1">Latitude of the first point in radians</param>
        /// <param name="λ1">Longitude of the first point in radians</param>
        /// <param name="φ2">Latitude of the second point in radians</param>
        /// <param name="λ2">Latitude of the second point in radians</param>
        /// <returns>The distance between the two points along a great circle in KM's</returns>
        public static double DistanceKM(double φ1, double λ1, double φ2, double λ2)
        {
            return 6371.0 * DistanceRadians(φ1,  λ1,  φ2,  λ2);
        }

        /// <summary>
        /// Returns the pythagoran distance between two pairs of points
        /// </summary>
        /// <param name="φ1">Latitude/x in anything for point 1</param>
        /// <param name="λ1">Longitude/y in anything for point 1</param>
        /// <param name="φ2">Latitude/x in anything for point 2</param>
        /// <param name="λ2">Longitude/y in anything for point 2</param>
        /// <returns>Pythagoran distance in units of the input units</returns>
        public static Double DistancePyth(double φ1, double λ1, double φ2, double λ2)
        {
            double dλ = λ1 - λ2;
            double dφ = φ1 - φ2;
            return Math.Sqrt(dλ * dλ + dφ * dφ);
        }

        /// <summary>
        /// Returns the bearing from point1 to point2
        /// </summary>
        /// <param name="φ1">Latitude of the first point in radians</param>
        /// <param name="λ1">Longitude of the first point in radians</param>
        /// <param name="φ2">Latitude of the second point in radians</param>
        /// <param name="λ2">Latitude of the second point in radians</param>
        /// <returns>Returns the bearing from point1 to point2 in radians</returns>
        public static double BearingRadians(double φ1, double λ1, double φ2, double λ2)
        {
            double dλ = (λ2 - λ1);
            double dφ = Math.Log(Math.Tan(φ2 / 2 + Math.PI / 4) / Math.Tan(φ1 / 2 + Math.PI / 4));
            if (Math.Abs(dλ) > Math.PI)
            {
                dλ = dλ > 0 ? -(2 * Math.PI - dλ) : (2 * Math.PI + dλ);
            }
            return Math.Atan2(dλ, dφ);
        }

        /// <summary>
        /// Computes where two lines intersect. Lines are given as pairs of lat/lon + bearing
        /// Returns false if no point can be computed, might return the intersection on the other side of the earth
        /// </summary>
        /// <param name="φ1">Latitude of the first point in radians</param>
        /// <param name="λ1">Longitude of the first point in radians</param>
        /// <param name="θ13">Bearing from the first point</param>
        /// <param name="φ2">Latitude of the second point in radians</param>
        /// <param name="λ2">Longitude of the second point in radians</param>
        /// <param name="θ23">Bearing from the second point</param>
        /// <param name="φi"></param>
        /// <param name="λi"></param>
        /// <returns>True if Lat/Lon in radians for one of the intersections could be computed</returns>
        public static bool Intersection(double φ1, double λ1, double θ13, double φ2, double λ2, double θ23, ref double φi, ref double λi)
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
            φi = Math.Asin(sinφ1 * Math.Cos(δ13) + cosφ1 * Math.Sin(δ13) * Math.Cos(θ13));
            double Δλ13 = Math.Atan2(Math.Sin(θ13) * Math.Sin(δ13) * cosφ1, Math.Cos(δ13) - sinφ1 * Math.Sin(φi));
            λi = (((λ1 + Δλ13) + 3.0 * Math.PI) % (VortexMath.PI2)) - Math.PI;

            return true;
        }

        public static void PointFrom(double φ1, double λ1, double θ, double distance, ref double φo, ref double λo)
        {
            double dR = distance / 6371.0;
            double sinφ1 = Math.Sin(φ1);
            double cosφ1 = Math.Cos(φ1);
            double sindR = Math.Sin(dR);
            double cosdR = Math.Cos(dR);
            φo = Math.Asin(sinφ1 * cosdR + cosφ1 * sindR * Math.Cos(θ));
            λo = λ1 + Math.Atan2(Math.Sin(θ) * sindR * cosφ1, cosdR - sinφ1 * Math.Sin(φo));
        }

        /// <summary>
        /// Calculates the number of radians to turn from θ1 ('current bearing') to θ2 ('next bearing')
        /// Value is returned in range [0...180>, so it does not show the direction, just the 'sharpness'
        /// </summary>
        /// <param name="θ1">Current Bearing</param>
        /// <param name="θ2">Next Bearing</param>
        /// <returns></returns>
        public static double TurnAngle(double θ1, double θ2)
        {
            return Math.Abs(((VortexMath.PI3 + θ1 - θ2) % VortexMath.PI2) - VortexMath.PI);
        }
    }
}
