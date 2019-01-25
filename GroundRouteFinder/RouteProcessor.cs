using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder
{
    public static class RouteProcessor
    {
        static RouteProcessor()
        {
        }

        public static void ProcessRunwayOperations(List<SteerPoint> steerPoints)
        {
            for (int i = 1; i < steerPoints.Count(); i++)
            {
                if (steerPoints[i - 1] is RunwayPoint && steerPoints[i] is RunwayPoint)
                {
                    RunwayPoint prev = steerPoints[i - 1] as RunwayPoint;
                    RunwayPoint curr = steerPoints[i] as RunwayPoint;

                    // When entering an active zone, mark it as hold short
                    // When staying in the same active zone, mark it as 'on runway'
                    // When switching to a new active zone, try to force a hold short
                    // todo: will this work as intended?
                    curr.OnRunway = (prev.Operations == curr.Operations);

                    // Propagate IsExiting for as long as we are on a runway.
                    if (curr.OnRunway && prev.IsExiting)
                    {
                        curr.IsExiting = true;
                    }
                }
            }
        }

        public static void Smooth(List<SteerPoint> steerPoints)
        {
            for (int i = 1; i < steerPoints.Count()-1; i++)
            {
                if (steerPoints[i] is PushbackPoint)
                    continue;

                SteerPoint previous = steerPoints[i - 1];
                SteerPoint current = steerPoints[i];
                SteerPoint next = steerPoints[i + 1];

                double incomingBearing = VortexMath.BearingRadians(previous, current);
                double outgoingBearing = VortexMath.BearingRadians(current, next);
                double turnAngle = VortexMath.AbsTurnAngle(incomingBearing, outgoingBearing);

                if (!current.Protected && turnAngle < 3.5 * VortexMath.Deg2Rad)
                {
                    if (previous.Name == next.Name && previous.Speed == next.Speed)
                    {
                        steerPoints.RemoveAt(i);
                        i--;
                    }
                }
                else if (!current.Protected && turnAngle > VortexMath.PI025) // 45 degrees
                {
                    double smoothingDistance = 0.050 * (turnAngle / VortexMath.PI); // 90 degrees = 0.5 PI / PI = 0.5 * 0.05 km = 25 meters
                    double currentLatitude = current.Latitude;
                    double currentLongitude = current.Longitude;
                    if (VortexMath.DistanceKM(previous.Latitude, previous.Longitude, currentLatitude, currentLongitude) > smoothingDistance)
                    {
                        // Shift the current point a bit back
                        VortexMath.PointFrom(currentLatitude, currentLongitude, incomingBearing + VortexMath.PI, smoothingDistance, ref current.Latitude, ref current.Longitude);
                        current.Speed /= 2;
                        //current.Name = "Shifted from " + current.Name;
                    }
                    else
                    {
                        // skip the current
                        steerPoints.RemoveAt(i);
                        i--;
                    }

                    double distanceToNext = VortexMath.DistanceKM(currentLatitude, currentLongitude, next.Latitude, next.Longitude);
                    if (distanceToNext > smoothingDistance)
                    {
                        // Insert an extra point
                        SteerPoint newPoint = next.Duplicate();
                        VortexMath.PointFrom(currentLatitude, currentLongitude, outgoingBearing, smoothingDistance, ref newPoint.Latitude, ref newPoint.Longitude);

                        // If room for additional speed up point, reduce acceleration on this point
                        if (distanceToNext > 1.5 * smoothingDistance)
                            newPoint.Speed = (newPoint.Speed * 2)/ 3;

                        steerPoints.Insert(i + 1, newPoint);
                        i++;
                    }

                    if (distanceToNext > 1.5 * smoothingDistance)
                    {
                        // Insert an extra point
                        SteerPoint newPoint = next.Duplicate();
                        VortexMath.PointFrom(currentLatitude, currentLongitude, outgoingBearing, 1.5 * smoothingDistance, ref newPoint.Latitude, ref newPoint.Longitude);
                        steerPoints.Insert(i + 1, newPoint);
                        i++;
                    }

                }
            }
        }
    }
}
