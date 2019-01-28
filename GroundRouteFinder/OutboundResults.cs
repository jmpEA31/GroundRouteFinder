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

        public static int MaxOutPoints = 0;

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
                                sw.Write($"STARTAIRCRAFTTYPE\n{allSizes}\nENDAIRCRAFTTYPE\n\n");
                                sw.Write("STARTCARGO\n0\nENDCARGO\n\n");
                                sw.Write("STARTMILITARY\n0\nENDMILITARY\n\n");
                                sw.Write($"STARTRUNWAY\n{Runway.Designator}\nENDRUNWAY\n\n");
                                sw.Write("START_PARKING_CENTER\nNOSEWHEEL\nEND_PARKING_CENTER\n\n");
                                sw.Write("STARTSTEERPOINTS\n");

                                IEnumerable<SteerPoint> steerPoints = buildSteerPoints(currentParking, route);

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

        public void WriteRoutesKML()
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
                            string fileName = $"{Settings.DepartureFolder}\\LFPG\\{currentParking.FileNameSafeName}_to_{Runway.Designator}-{route.TargetNode.Id}_{sizeName}.kml";
                            File.Delete(fileName);

                            using (StreamWriter sw = File.CreateText(fileName))
                            {
                                LinkedNode link = route.RouteStart;
                                TaxiNode nodeToWrite = route.NearestNode;
                                RunwayTakeOffSpot takeoffSpot = route.TakeoffSpot;

                                sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                                sw.WriteLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");
                                sw.WriteLine("<Document>");
                                sw.WriteLine("<Style id=\"Parking\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/shapes/parking_lot.png</href></Icon></IconStyle></Style>");
                                sw.WriteLine("<Style id=\"HoldShort\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/shapes/hospitals.png</href></Icon></IconStyle></Style>");
                                sw.WriteLine("<Style id=\"Runway\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/pal4/icon49.png</href></Icon></IconStyle></Style>");
                                sw.WriteLine("<Style id=\"Pushback\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/paddle/grn-stars-lv.png</href></Icon></IconStyle></Style>");
                                sw.WriteLine("<Style id=\"TaxiNode\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/paddle/grn-blank-lv.png</href></Icon></IconStyle></Style>");
                                sw.WriteLine("<Style id=\"TaxiLine\"><LineStyle><color>ff0000ff</color><width>3</width></LineStyle></Style>");

                                IEnumerable<SteerPoint> steerPoints = buildSteerPoints(currentParking, route);

                                StringBuilder coords = new StringBuilder();
                                foreach (SteerPoint steerPoint in steerPoints)
                                {
                                    steerPoint.WriteKML(sw);
                                    coords.Append($"  {steerPoint.Longitude * VortexMath.Rad2Deg},{steerPoint.Latitude * VortexMath.Rad2Deg},0\n");
                                }

                                sw.WriteLine($"<Placemark><styleUrl>#TaxiLine</styleUrl><LineString>\n<coordinates>\n{coords.ToString()}</coordinates>\n</LineString></Placemark>\n");

                                sw.WriteLine("</Document>");
                                sw.WriteLine("</kml>");
                            }
                        }
                        // DEBUG: ONly one route for all sizes
                        break;
                    }
                }
            }
        }

        private IEnumerable<SteerPoint> buildSteerPoints(Parking currentParking, ResultRoute route)
        {
            LinkedNode link = route.RouteStart;
            TaxiNode nodeToWrite = route.NearestNode;
            RunwayTakeOffSpot takeoffSpot = route.TakeoffSpot;

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

            // insert one more point here where the plane is pushed a little bit away from the next point
            if (nodeToWrite != null)
            {
                double nextPushBearing = VortexMath.BearingRadians(nodeToWrite.Latitude, nodeToWrite.Longitude, currentParking.PushBackLatitude, currentParking.PushBackLongitude);
                double turn = VortexMath.TurnAngle(currentParking.Bearing + VortexMath.PI, nextPushBearing);
                double turnAbs = Math.Abs(turn);
                double factor = ((turnAbs) / VortexMath.PI);  // 0...0.5.....1
                factor = (factor * factor) + factor / 4;                   // 0...0.375...1.25
                double distance = 0.040 * factor;                          // 0m...15m ...50m

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

            if (nodeToWrite != link.Node)
                steerPoints.Add(new SteerPoint(nodeToWrite.Latitude, nodeToWrite.Longitude, 8, $"{link.Edge.LinkName}"));

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

            if (MaxOutPoints < steerPoints.Count)
                MaxOutPoints = steerPoints.Count;

            return steerPoints;
        }
    }
}
