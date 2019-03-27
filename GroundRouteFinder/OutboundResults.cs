﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GroundRouteFinder.AptDat;
using GroundRouteFinder.LogSupport;
using GroundRouteFinder.Output;

namespace GroundRouteFinder
{
    public class OutboundResults
    {
        public Runway Runway;
        private readonly IEnumerable<TaxiEdge> _edges;
        private Dictionary<TaxiNode, Dictionary<XPlaneAircraftCategory, ResultRoute>> _results;

        public static int MaxOutPoints = 0;

        public OutboundResults(IEnumerable<TaxiEdge> edges, Runway runway)
        {
            _edges = edges;
            Runway = runway;
            _results = new Dictionary<TaxiNode, Dictionary<XPlaneAircraftCategory, ResultRoute>>();
        }

        /// <summary>
        /// Add a resulting route for a specific runway exit and size
        /// </summary>
        /// <param name="maxSizeCurrentResult">The maximum size allowed on the current route</param>
        /// <param name="parkingNode">The runway node for this exit</param>
        public void AddResult(XPlaneAircraftCategory maxSizeCurrentResult, TaxiNode parkingNode, Parking parking, TaxiNode entryGroupNode, EntryPoint entryPoint)
        {
            if (!_results.ContainsKey(parkingNode))
                _results[parkingNode] = new Dictionary<XPlaneAircraftCategory, ResultRoute>();

            Dictionary<XPlaneAircraftCategory, ResultRoute> originResults = _results[parkingNode];

            // If no results yet for this node, just add the current route
            if (originResults.Count == 0)
            {
                ResultRoute theRoute = ResultRoute.ExtractRoute(_edges, parkingNode, maxSizeCurrentResult);
                if (!theRoute.HasNode(entryPoint.OnRunwayNode))
                {
                    originResults.Add(maxSizeCurrentResult, theRoute);
                    originResults[maxSizeCurrentResult].RunwayEntryPoint = entryPoint;
                    originResults[maxSizeCurrentResult].AvailableRunwayLength = entryPoint.TakeoffLengthRemaining;
                }
            }
            else
            {
                XPlaneAircraftCategory minSize = originResults.Min(or => or.Key);
                if (originResults[minSize].Distance > parkingNode.DistanceToTarget)
                {
                    if (minSize > maxSizeCurrentResult)
                    {
                        ResultRoute theRoute = ResultRoute.ExtractRoute(_edges, parkingNode, maxSizeCurrentResult);
                        if (!theRoute.HasNode(entryPoint.OnRunwayNode))
                        {
                            originResults.Add(maxSizeCurrentResult, theRoute);
                            originResults[maxSizeCurrentResult].RunwayEntryPoint = entryPoint;
                            originResults[maxSizeCurrentResult].AvailableRunwayLength = entryPoint.TakeoffLengthRemaining;

                            originResults[minSize].MinSize = (maxSizeCurrentResult + 1);
                        }
                    }
                    else if (minSize == maxSizeCurrentResult)
                    {
                        ResultRoute theRoute = ResultRoute.ExtractRoute(_edges, parkingNode, maxSizeCurrentResult);
                        if (!theRoute.HasNode(entryPoint.OnRunwayNode))
                        {
                            originResults[minSize] = theRoute;
                            originResults[minSize].RunwayEntryPoint = entryPoint;
                            originResults[minSize].AvailableRunwayLength = entryPoint.TakeoffLengthRemaining;
                        }
                    }
                }
            }

            // Nasty overkill to make sure parkings with the same 'nearest' node will have routes generated
            foreach (KeyValuePair<XPlaneAircraftCategory, ResultRoute> result in originResults)
            {
                result.Value.AddParking(parking);
            }
        }

        public int WriteRoutes(string outputPath, bool kml)
        {
            int count = 0;

            foreach (KeyValuePair<TaxiNode, Dictionary<XPlaneAircraftCategory, ResultRoute>> sizeRoutes in _results)
            {
                for (XPlaneAircraftCategory size = XPlaneAircraftCategory.Max - 1; size >= XPlaneAircraftCategory.A; size--)
                {
                    if (sizeRoutes.Value.ContainsKey(size))
                    {
                        ResultRoute route = sizeRoutes.Value[size];
                        if (route.TargetNode == null)
                            continue;

                        if (route.AvailableRunwayLength < VortexMath.Feet3000Km)
                            continue;

                        foreach (Parking currentParking in route.Parkings)
                        {
                            IEnumerable<WorldTrafficAircraftType> wtTypes = AircraftTypeConverter.WTTypesFromXPlaneLimits(XPlaneAircraftCategory.A, route.MaxSize, currentParking.Operation);

                            if (route.AvailableRunwayLength < VortexMath.Feet9000Km)
                            {
                                WorldTrafficAircraftType[] big = { WorldTrafficAircraftType.SuperHeavy, WorldTrafficAircraftType.HeavyJet };
                                wtTypes = wtTypes.Except(big);
                            }
                            if (route.AvailableRunwayLength < VortexMath.Feet6500Km)
                            {
                                WorldTrafficAircraftType[] big = { WorldTrafficAircraftType.LargeJet };
                                wtTypes = wtTypes.Except(big);
                            }
                            if (route.AvailableRunwayLength < VortexMath.Feet5000Km)
                            {
                                WorldTrafficAircraftType[] big = { WorldTrafficAircraftType.MediumJet, WorldTrafficAircraftType.LightJet };
                                wtTypes = wtTypes.Except(big);
                            }
                            if (route.AvailableRunwayLength < VortexMath.Feet4000Km)
                            {
                                WorldTrafficAircraftType[] big = { WorldTrafficAircraftType.LargeProp, WorldTrafficAircraftType.MediumProp };
                                wtTypes = wtTypes.Except(big);
                            }

                            if (wtTypes.Count() == 0)
                                continue;

                            IEnumerable<SteerPoint> steerPoints = BuildSteerPoints(currentParking, route);

                            if (steerPoints.Count() <= Settings.MaxSteerpoints)
                            {
                                string allSizes = string.Join(" ", wtTypes.Select(w => (int)w).OrderBy(w => w));
                                string sizeName = (wtTypes.Count() == 10) ? "all" : allSizes.Replace(" ", "");
                                string fileName = Path.Combine(outputPath, $"{currentParking.FileNameSafeName}_to_{Runway.Designator}-{route.TargetNode.Id}_{sizeName}");

                                int military = (currentParking.Operation == OperationType.Military) ? 1 : 0;
                                int cargo = (currentParking.Operation == OperationType.Cargo) ? 1 : 0;

                                using (RouteWriter sw = RouteWriter.Create(kml ? 0 : 1, fileName, allSizes, cargo, military, Runway.Designator, "NOSEWHEEL"))
                                {
                                    count++;

                                    foreach (SteerPoint steerPoint in steerPoints)
                                    {
                                        sw.Write(steerPoint);
                                    }
                                }
                            }
                            else
                            {
                                Logger.Log($"Route from <{currentParking.FileNameSafeName}> to {Runway.Designator} not written. Too many steerpoints ({steerPoints.Count()} vs {Settings.MaxSteerpoints})");
                            }
                        }
                    }
                }
            }

            return count;
        }

        private IEnumerable<SteerPoint> BuildSteerPoints(Parking currentParking, ResultRoute route)
        {
            LinkedNode link = route.RouteStart;
            TaxiNode nodeToWrite = route.StartNode;
            EntryPoint entryPoint = route.RunwayEntryPoint;

            List<SteerPoint> steerPoints = new List<SteerPoint>
            {
                new ParkingPoint(currentParking.Latitude, currentParking.Longitude, 3, $"{currentParking.Name}", currentParking.Bearing, false)
            };

            // Write Pushback node, allowing room for turn
            double addLat = 0;
            double addLon = 0;

            // See if we need to skip the first route node
            if (currentParking.AlternateAfterPushBack != null && currentParking.AlternateAfterPushBack == route.RouteStart.Node)
            {
                // Our pushback point is better than the first point of the route
                nodeToWrite = currentParking.AlternateAfterPushBack;
            }

            // insert one more point here where the plane is pushed a little bit away from the next point
            if (currentParking.LocationType == StartUpLocationType.Gate)
            {
                if (nodeToWrite != null)
                {
                    double nextPushBearing;

                    if (VortexMath.DistanceKM(nodeToWrite.Latitude, nodeToWrite.Longitude, currentParking.PushBackLatitude, currentParking.PushBackLongitude) > 0.010)
                    {
                        // Push target is a virtual node
                        nextPushBearing = VortexMath.BearingRadians(nodeToWrite.Latitude, nodeToWrite.Longitude, currentParking.PushBackLatitude, currentParking.PushBackLongitude);
                    }
                    else
                    {
                        // Push target is very close to the actual first node of the route
                        nextPushBearing = (nodeToWrite.BearingToTarget + VortexMath.PI) % VortexMath.PI2;
                    }

                    double turn = VortexMath.TurnAngle(currentParking.Bearing + VortexMath.PI, nextPushBearing);
                    double turnAbs = Math.Abs(turn);
                    double factor = ((turnAbs) / VortexMath.PI);                // 0...0.5.....1
                    factor = (factor * factor) + factor / 4;                    // 0...0.375...1.25
                    double distance = 0.040 * factor;                           // 0m...15m ...50m

                    if (turnAbs < VortexMath.Deg135Rad)
                    {
                        // Try to trun the aircraft to the bearing it will need to go in after pushback

                        // First point is on the pushback heading, but away from the actual target to allow the AC to turn
                        VortexMath.PointFrom(currentParking.PushBackLatitude, currentParking.PushBackLongitude, currentParking.Bearing, distance, ref addLat, ref addLon);
                        steerPoints.Add(new PushbackPoint(addLat, addLon, 2, $"{currentParking.Name}"));

                        // Second point is on the (extended) line of the first link of the actual route
                        VortexMath.PointFrom(currentParking.PushBackLatitude, currentParking.PushBackLongitude, nextPushBearing, distance, ref addLat, ref addLon);
                        steerPoints.Add(new PushbackPoint(addLat, addLon, 2, $"{link.Edge.LinkName}"));

                        // Third point is on the same line but a little bit extra backwards to get the nose in the intended heading
                        VortexMath.PointFrom(currentParking.PushBackLatitude, currentParking.PushBackLongitude, nextPushBearing, distance + 0.015, ref addLat, ref addLon);
                        steerPoints.Add(new SteerPoint(addLat, addLon, 8, $"{link.Edge.LinkName}", true));
                    }
                    else
                    {
                        // Let's just turn it to a 90 degree angle with the first edge

                        // First point is on the pushback heading, but away from the actual target to allow the AC to turn
                        VortexMath.PointFrom(currentParking.PushBackLatitude, currentParking.PushBackLongitude, currentParking.Bearing, distance, ref addLat, ref addLon);
                        steerPoints.Add(new PushbackPoint(addLat, addLon, 2, $"{currentParking.Name}"));

                        // Second point is on the (extended) line of the first link of the actual route, but much closer then for the full turn
                        VortexMath.PointFrom(currentParking.PushBackLatitude, currentParking.PushBackLongitude, nextPushBearing, distance / 2.0, ref addLat, ref addLon);
                        steerPoints.Add(new PushbackPoint(addLat, addLon, 2, $"{link.Edge.LinkName}"));

                        // Third point is on +/-90 degree angle from the first link
                        VortexMath.PointFrom(addLat, addLon, (turn > 0) ? nextPushBearing + VortexMath.PI05 : nextPushBearing - VortexMath.PI05, 0.015, ref addLat, ref addLon);
                        steerPoints.Add(new SteerPoint(addLat, addLon, 5, $"{link.Edge.LinkName}", true));

                        // Add a fourth point back on the intended line
                        steerPoints.Add(new SteerPoint(currentParking.PushBackLatitude, currentParking.PushBackLongitude, 8, $"{link.Edge.LinkName}"));
                    }
                }
            }
            else
            {
                // Tie down, hangar, misc: just add the 'pushback' point as first target, smoothing should take care of the rest
                steerPoints.Add(new SteerPoint(currentParking.PushBackLatitude, currentParking.PushBackLongitude, 8, $"{link.Edge.LinkName}"));
            }

            if (nodeToWrite != link.Node)
                steerPoints.Add(new SteerPoint(nodeToWrite.Latitude, nodeToWrite.Longitude, 8, $"{link.Edge.LinkName}"));

            while (link.Node != null)
            {
                bool activeZone = false;
                string activeFor = "";

                if (link.Edge.ActiveZone)
                {
                    activeZone = true;
                    activeFor = link.Edge.ActiveForRunway(Runway.Designator);
                }
                else if (link.Next.Edge != null && link.Next.Edge.ActiveZone)
                {
                    activeZone = true;
                    activeFor = link.Next.Edge.ActiveForRunway(Runway.Designator);
                }
                else if (link.Next.Edge == null)
                {
                    activeZone = true;
                    activeFor = Runway.Designator;
                }

                if (activeZone)
                    steerPoints.Add(new RunwayPoint(link.Node.Latitude, link.Node.Longitude, 15, $"{link.Edge.LinkName}", activeFor));
                else
                    steerPoints.Add(new SteerPoint(link.Node.Latitude, link.Node.Longitude, 15, $"{link.Edge.LinkName}"));

                link = link.Next;
            }

            steerPoints.Add(new RunwayPoint(entryPoint.OnRunwayNode, 8, Runway.Designator, Runway.Designator));

            VortexMath.PointFrom(entryPoint.OnRunwayNode, Runway.Bearing, 0.022, ref addLat, ref addLon);
            steerPoints.Add(new RunwayPoint(addLat, addLon, 6, Runway.Designator, Runway.Designator));

            RouteProcessor.Smooth(steerPoints);
            RouteProcessor.ProcessRunwayOperations(steerPoints);

            if (MaxOutPoints < steerPoints.Count)
                MaxOutPoints = steerPoints.Count;

            return steerPoints;
        }
    }
}
