using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GroundRouteFinder.AptDat;

namespace GroundRouteFinder
{
    public class OutboundResults
    {
        public Runway Runway;
        private IEnumerable<TaxiEdge> _edges;
        private Dictionary<TaxiNode, Dictionary<int, ResultRoute>> _results;

        public OutboundResults(IEnumerable<TaxiEdge> edges, Runway runway)
        {
            _edges = edges;
            Runway = runway;
            _results = new Dictionary<TaxiNode, Dictionary<int, ResultRoute>>();
        }

        /// <summary>
        /// Add a resulting route for a specific runway exit and size
        /// </summary>
        /// <param name="maxSizeCurrentResult">The maximum size allowed on the current route</param>
        /// <param name="parkingNode">The runway node for this exit</param>
        public void AddResult(int maxSizeCurrentResult, TaxiNode parkingNode, Parking parking, RunwayTakeOffSpot takeOffSpot)
        {
            if (!_results.ContainsKey(parkingNode))
                _results[parkingNode] = new Dictionary<int, ResultRoute>();

            Dictionary<int, ResultRoute> originResults = _results[parkingNode];

            // If no results yet for this node, just add the current route
            if (originResults.Count == 0)
            {
                originResults.Add(maxSizeCurrentResult, ResultRoute.ExtractRoute(_edges, parkingNode, maxSizeCurrentResult));
                originResults[maxSizeCurrentResult].TakeoffSpot = takeOffSpot;
            }
            else
            {
                int minSize = originResults.Min(or => or.Key);
                if (originResults[minSize].Distance > parkingNode.DistanceToTarget)
                {
                    if (minSize > maxSizeCurrentResult)
                    {
                        originResults.Add(maxSizeCurrentResult, ResultRoute.ExtractRoute(_edges, parkingNode, maxSizeCurrentResult));
                        originResults[maxSizeCurrentResult].TakeoffSpot = takeOffSpot;
                        originResults[minSize].MinSize = (maxSizeCurrentResult + 1);
                    }
                    else if (minSize == maxSizeCurrentResult)
                    {
                        originResults[minSize] = ResultRoute.ExtractRoute(_edges, parkingNode, maxSizeCurrentResult);
                        originResults[minSize].TakeoffSpot = takeOffSpot;
                    }
                }
            }

            // Nsty overkill to make sure parkings with the same 'nearest' node will have routes generated
            foreach (KeyValuePair<int, ResultRoute> result in originResults)
            {
                result.Value.AddParking(parking);
            }
        }

        public void WriteRoutes()
        {
            foreach (KeyValuePair<TaxiNode, Dictionary<int, ResultRoute>> sizeRoutes in _results)
            {
                for (int size = TaxiNode.Sizes - 1; size >= 0; size--)
                {
                    if (sizeRoutes.Value.ContainsKey(size))
                    {
                        ResultRoute route = sizeRoutes.Value[size];
                        if (route.TargetNode == null)
                            continue;

                        List<int> routeSizes = new List<int>();
                        for (int s = route.MinSize; s <= route.MaxSize; s++)
                        {
                            routeSizes.AddRange(Settings.XPlaneCategoryToWTType(s));
                        }

                        string allSizes = string.Join(" ", routeSizes.OrderBy(w => w));
                        string sizeName = (routeSizes.Count == 10) ? "all" : allSizes.Replace(" ", "");

                        //Debug
                        allSizes = "0 1 2 3 4 5 6 7 8 9";
                        sizeName = "all";

                        foreach (Parking currentParking in route.Parkings)
                        {
                            string fileName = $"{Settings.DepartureFolder}\\LFPG\\{currentParking.FileNameSafeName}_to_{Runway.Designator}-{route.TargetNode.Id}_{sizeName}.txt";
                            File.Delete(fileName);
                            using (StreamWriter sw = File.CreateText(fileName))
                            {
                                LinkedNode link = route.RouteStart;
                                TaxiNode nodeToWrite = route.NearestNode;
                                RunwayTakeOffSpot takeoffSpot = route.TakeoffSpot;

                                sw.Write($"STARTAIRCRAFTTYPE\n{allSizes}\nENDAIRCRAFTTYPE\n\n");
                                sw.Write("STARTCARGO\n0\nENDCARGO\n\n");
                                sw.Write("STARTMILITARY\n0\nENDMILITARY\n\n");
                                sw.Write($"STARTRUNWAY\n{Runway.Designator}\nENDRUNWAY\n\n");
                                sw.Write("START_PARKING_CENTER\nNOSEWHEEL\nEND_PARKING_CENTER\n\n");
                                sw.Write("STARTSTEERPOINTS\n");

                                List<SteerPoint> steerPoints = new List<SteerPoint>();
                                steerPoints.Add(new ParkingPoint(currentParking.Latitude, currentParking.Longitude, 3, $"{currentParking.Name}", currentParking.Bearing, false));

                                // Write Pushback node, allowing room for turn
                                double addLat = 0;
                                double addLon = 0;

                                // See if we need to skip the first route node
                                if (currentParking.AlternateAfterPushBack != null && currentParking.AlternateAfterPushBack == route.RouteStart.Node)
                                {
                                    // Our pushback point is better than the first point of the route
                                    nodeToWrite = currentParking.AlternateAfterPushBack;
                                }
                                else if (VortexMath.DistancePyth(currentParking.PushBackLatitude, currentParking.PushBackLongitude, route.NearestNode.Latitude, route.NearestNode.Longitude) < 0.000000001)
                                {
                                    // pushback node is the first route point
                                    nodeToWrite = route.NearestNode;
                                }

                                // insert one more point here where the plane is pushed a little bit away from the next point
                                if (nodeToWrite != null)
                                {
                                    double nextPushBearing = VortexMath.BearingRadians(nodeToWrite.Latitude, nodeToWrite.Longitude, currentParking.PushBackLatitude, currentParking.PushBackLongitude);
                                    double turn = VortexMath.AbsTurnAngle(currentParking.Bearing, nextPushBearing);
                                    double factor = ((VortexMath.PI - turn) / VortexMath.PI);  // 0...0.5.....1
                                    factor = (factor * factor) + factor / 4;                   // 0...0.375...1.25
                                    double distance = 0.040 * factor;                          // 0m...15m ...50m

                                    VortexMath.PointFrom(currentParking.PushBackLatitude, currentParking.PushBackLongitude, currentParking.Bearing, distance, ref addLat, ref addLon);
                                    steerPoints.Add(new PushbackPoint(addLat, addLon, 2, $"{currentParking.Name}"));

                                    VortexMath.PointFrom(currentParking.PushBackLatitude, currentParking.PushBackLongitude, nextPushBearing, distance, ref addLat, ref addLon);
                                    steerPoints.Add(new PushbackPoint(addLat, addLon, 2, $"{link.Edge.LinkName}"));

                                    VortexMath.PointFrom(currentParking.PushBackLatitude, currentParking.PushBackLongitude, nextPushBearing, distance + 0.005, ref addLat, ref addLon);
                                    steerPoints.Add(new SteerPoint(addLat, addLon, 8, $"{link.Edge.LinkName}", true));
                                }

                                TaxiEdge lastEdge = null;
                                while (link.Node != null)
                                {
                                    lastEdge = link.Edge;
                                    if (link.Edge.ActiveZone)
                                        steerPoints.Add(new RunwayPoint(link.Node.Latitude, link.Node.Longitude, 15, $"{link.Edge.LinkName}", link.Edge.ActiveForRunway(Runway.Designator)));
                                    else
                                        steerPoints.Add(new SteerPoint(link.Node.Latitude, link.Node.Longitude, 15, $"{link.Edge.LinkName}"));

                                    link = link.Next;
                                }

                                steerPoints.Add(new RunwayPoint(takeoffSpot.TakeOffNode.Latitude, takeoffSpot.TakeOffNode.Longitude, 8, $"{Runway.Designator}", lastEdge.ActiveForRunway(Runway.Designator)));

                                VortexMath.PointFrom(takeoffSpot.TakeOffNode.Latitude, takeoffSpot.TakeOffNode.Longitude, Runway.Bearing, 0.022, ref addLat, ref addLon);
                                steerPoints.Add(new RunwayPoint(addLat, addLon, 6, $"{Runway.Designator}", lastEdge.ActiveForRunway(Runway.Designator)));

                                RouteProcessor.Smooth(steerPoints);
                                RouteProcessor.ProcessRunwayOperations(steerPoints);
                                foreach (SteerPoint steerPoint in steerPoints)
                                {
                                    steerPoint.Write(sw);
                                }

                                sw.Write("ENDSTEERPOINTS\n");
                            }
                        }
                        // DEBUG: ONly one route for all sizes
                        break;
                    }
                }
            }
        }
    }
}
