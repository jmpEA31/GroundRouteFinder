﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GroundRouteFinder.AptDat;

namespace GroundRouteFinder
{
    public class InboundResults
    {
        public Parking Parking;

        private IEnumerable<TaxiEdge> _edges;
        private Dictionary<TaxiNode, Dictionary<XPlaneAircraftCategory, ResultRoute>> _results;

        public static int MaxInPoints = 0;

        public InboundResults(IEnumerable<TaxiEdge> edges, Parking parking)
        {
            _edges = edges;
            Parking = parking;
            _results = new Dictionary<TaxiNode, Dictionary<XPlaneAircraftCategory, ResultRoute>>();
        }

        /// <summary>
        /// Add a resulting route for a specific runway exit and size
        /// </summary>
        /// <param name="maxSizeCurrentResult">The maximum size allowed on the current route</param>
        /// <param name="runwayExitNode">The runway node for this exit</param>
        /// <param name="pathStartNode">The frist node 'departing' the runway</param>
        /// <param name="r">The runway it self</param>
        public void AddResult(XPlaneAircraftCategory maxSizeCurrentResult, TaxiNode runwayExitNode, TaxiNode pathStartNode, Runway r, double availableRunwayLength)
        {
            if (!_results.ContainsKey(runwayExitNode))
                _results[runwayExitNode] = new Dictionary<XPlaneAircraftCategory, ResultRoute>();

            Dictionary<XPlaneAircraftCategory, ResultRoute> originResults = _results[runwayExitNode];

            // If no results yet for this node, just add the current route
            if (originResults.Count == 0)
            {
                originResults.Add(maxSizeCurrentResult, ResultRoute.ExtractRoute(_edges, pathStartNode, maxSizeCurrentResult));
                originResults[maxSizeCurrentResult].Runway = r;
                originResults[maxSizeCurrentResult].AvailableRunwayLength = availableRunwayLength;
            }
            else
            {
                XPlaneAircraftCategory minSize = originResults.Min(or => or.Key);
                if (originResults[minSize].Distance > pathStartNode.DistanceToTarget)
                {
                    if (minSize > maxSizeCurrentResult)
                    {
                        originResults.Add(maxSizeCurrentResult, ResultRoute.ExtractRoute(_edges, pathStartNode, maxSizeCurrentResult));
                        originResults[maxSizeCurrentResult].Runway = r;
                        originResults[maxSizeCurrentResult].AvailableRunwayLength = availableRunwayLength;
                        originResults[minSize].MinSize = (maxSizeCurrentResult + 1);
                    }
                    else if (minSize == maxSizeCurrentResult)
                    {
                        originResults[minSize] = ResultRoute.ExtractRoute(_edges, pathStartNode, maxSizeCurrentResult);
                        originResults[minSize].Runway = r;
                        originResults[minSize].AvailableRunwayLength = availableRunwayLength;
                    }
                }
            }
        }

        public void WriteRoutes(string outputPath)
        {
            foreach (KeyValuePair<TaxiNode, Dictionary<XPlaneAircraftCategory, ResultRoute>> sizeRoutes in _results)
            {
                for (XPlaneAircraftCategory size = Parking.MaxSize; size >= XPlaneAircraftCategory.A; size--)
                {
                    if (sizeRoutes.Value.ContainsKey(size))
                    {
                        ResultRoute route = sizeRoutes.Value[size];

                        if (route.TargetNode == null)
                            continue;

                        if (Parking.MaxSize < route.MinSize)
                            continue;

                        XPlaneAircraftCategory validMax = (XPlaneAircraftCategory)Math.Min((int)route.MaxSize, (int)Parking.MaxSize);
                        IEnumerable<WorldTrafficAircraftType> wtTypes = AircraftTypeConverter.WTTypesFromXPlaneLimits(route.MinSize, validMax, Parking.Operation);
                        if (wtTypes.Count() == 0)
                        {
                            Console.WriteLine($"WARN {Parking.Name} (Max)Cat {Parking.MaxSize} Types: {string.Join(" ", Parking.XpTypes)} does not map to any WT types.");
                        }

                        if (route.AvailableRunwayLength < VortexMath.Feet5000Km)
                        {
                            WorldTrafficAircraftType[] big = { WorldTrafficAircraftType.SuperHeavy, WorldTrafficAircraftType.HeavyJet, WorldTrafficAircraftType.LargeJet, WorldTrafficAircraftType.LargeProp, WorldTrafficAircraftType.LightJet };
                            wtTypes = wtTypes.Except(big);
                        }
                        else if (route.AvailableRunwayLength < VortexMath.Feet6500Km)
                        {
                            WorldTrafficAircraftType[] big = { WorldTrafficAircraftType.SuperHeavy, WorldTrafficAircraftType.HeavyJet };
                            wtTypes = wtTypes.Except(big);
                        }
                        else if (route.AvailableRunwayLength > VortexMath.Feet8000Km)
                        {
                            WorldTrafficAircraftType[] small = { WorldTrafficAircraftType.LightProp, WorldTrafficAircraftType.LightJet, WorldTrafficAircraftType.MediumProp };
                            wtTypes = wtTypes.Except(small);
                        }

                        if (wtTypes.Count() == 0)
                            continue;

                        string allSizes = string.Join(" ", wtTypes.Select(w=>(int)w).OrderBy(w => w));
                        string sizeName = (wtTypes.Count() == 10) ? "all" : allSizes.Replace(" ", "");

                        string fileName = $"{outputPath}\\{route.Runway.Designator}_to_{Parking.FileNameSafeName}_{route.RouteStart.Node.NameToTarget}_{sizeName}.txt";
                        using (StreamWriter sw = File.CreateText(fileName))
                        {
                            sw.Write($"STARTAIRCRAFTTYPE\n{allSizes}\nENDAIRCRAFTTYPE\n\n");
                            sw.Write("START_PARKING_CENTER\nNOSEWHEEL\nEND_PARKING_CENTER\n\n");
                            sw.Write($"STARTRUNWAY\n{route.Runway.Designator}\nENDRUNWAY\n\n");
                            sw.Write("STARTSTEERPOINTS\n");

                            IEnumerable<SteerPoint> steerPoints = buildSteerPoints(route, sizeRoutes.Key);

                            foreach (SteerPoint steerPoint in steerPoints)
                            {
                                steerPoint.Write(sw);
                            }

                            sw.Write("ENDSTEERPOINTS\n");
                        }
                    }
                }
            }
        }

        public void WriteRoutesKML(string outputPath)
        {
            foreach (KeyValuePair<TaxiNode, Dictionary<XPlaneAircraftCategory, ResultRoute>> sizeRoutes in _results)
            {
                for (XPlaneAircraftCategory size = Parking.MaxSize; size >= XPlaneAircraftCategory.A; size--)
                {
                    if (sizeRoutes.Value.ContainsKey(size))
                    {
                        ResultRoute route = sizeRoutes.Value[size];

                        if (route.TargetNode == null)
                            continue;

                        if (Parking.MaxSize < route.MinSize)
                            continue;

                        XPlaneAircraftCategory validMax = (XPlaneAircraftCategory)Math.Min((int)route.MaxSize, (int)Parking.MaxSize);
                        IEnumerable<WorldTrafficAircraftType> wtTypes = AircraftTypeConverter.WTTypesFromXPlaneLimits(route.MinSize, validMax, Parking.Operation);
                        if (wtTypes.Count() == 0)
                        {
                            Console.WriteLine($"WARN {Parking.Name} (Max)Cat {Parking.MaxSize} Types: {string.Join(" ", Parking.XpTypes)} does not map to any WT types.");
                        }

                        if (route.AvailableRunwayLength < VortexMath.Feet5000Km)
                        {
                            WorldTrafficAircraftType[] big = { WorldTrafficAircraftType.SuperHeavy, WorldTrafficAircraftType.HeavyJet, WorldTrafficAircraftType.LargeJet, WorldTrafficAircraftType.LargeProp, WorldTrafficAircraftType.LightJet };
                            wtTypes = wtTypes.Except(big);
                        }
                        else if (route.AvailableRunwayLength < VortexMath.Feet6500Km)
                        {
                            WorldTrafficAircraftType[] big = { WorldTrafficAircraftType.SuperHeavy, WorldTrafficAircraftType.HeavyJet };
                            wtTypes = wtTypes.Except(big);
                        }
                        else if (route.AvailableRunwayLength > VortexMath.Feet8000Km)
                        {
                            WorldTrafficAircraftType[] small = { WorldTrafficAircraftType.LightProp, WorldTrafficAircraftType.LightJet, WorldTrafficAircraftType.MediumProp };
                            wtTypes = wtTypes.Except(small);
                        }

                        if (wtTypes.Count() == 0)
                            continue;

                        string allSizes = string.Join(" ", wtTypes.Select(w => (int)w).OrderBy(w => w));
                        string sizeName = (wtTypes.Count() == 10) ? "all" : allSizes.Replace(" ", "");

                        string fileName = $"{outputPath}\\{route.Runway.Designator}_to_{Parking.FileNameSafeName}_{route.RouteStart.Node.NameToTarget}_{sizeName}.kml";
                        File.Delete(fileName);
                        using (StreamWriter sw = File.CreateText(fileName))
                        {
                            sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                            sw.WriteLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");
                            sw.WriteLine("<Document>");
                            sw.WriteLine("<Style id=\"Parking\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/shapes/parking_lot.png</href></Icon></IconStyle></Style>");
                            sw.WriteLine("<Style id=\"HoldShort\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/shapes/hospitals.png</href></Icon></IconStyle></Style>");
                            sw.WriteLine("<Style id=\"Runway\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/pal4/icon49.png</href></Icon></IconStyle></Style>");
                            sw.WriteLine("<Style id=\"Pushback\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/paddle/grn-stars-lv.png</href></Icon></IconStyle></Style>");
                            sw.WriteLine("<Style id=\"TaxiNode\"><IconStyle><Icon><href>http://maps.google.com/mapfiles/kml/paddle/grn-blank-lv.png</href></Icon></IconStyle></Style>");
                            sw.WriteLine("<Style id=\"TaxiLine\"><LineStyle><color>ff0000ff</color><width>3</width></LineStyle></Style>");

                            IEnumerable<SteerPoint> steerPoints = buildSteerPoints(route, sizeRoutes.Key);

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
                }
            }
        }

        private IEnumerable<SteerPoint> buildSteerPoints(ResultRoute route, TaxiNode runwayExitNode)
        {
            List<SteerPoint> steerPoints = new List<SteerPoint>();

            // Route should start at the (displaced) threshold
            RunwayPoint threshold = new RunwayPoint(route.Runway.DisplacedNode, 55, $"{route.Runway.Designator} Threshold", route.RouteStart.Edge.ActiveForRunway(route.Runway.Designator));
            threshold.OnRunway = true;
            threshold.IsExiting = true;
            steerPoints.Add(threshold);

            foreach (TaxiNode node in route.Runway.RunwayNodes)
            {
                int speed = (node == runwayExitNode) ? 35 : 55;
                steerPoints.Add(new RunwayPoint(node.Latitude, node.Longitude, speed, $"{route.Runway.Designator}", route.RouteStart.Edge.ActiveForRunway(route.Runway.Designator)));

                if (node == runwayExitNode) // Key of the dictionary is the last node on the runway centerline for this route
                    break;
            }

            // This is the first node off the runway centerline
            steerPoints.Add(new RunwayPoint(route.StartNode, 30, route.RouteStart.Edge.LinkName, route.RouteStart.Edge.ActiveForRunway(route.Runway.Designator)));

            LinkedNode link = route.RouteStart;
            while (link.Node != null)
            {
                if (link.Edge.ActiveZone)
                    steerPoints.Add(new RunwayPoint(link.Node, 15, $"{link.Edge.LinkName}", $"{link.Edge.ActiveForRunway(route.Runway.Designator)}"));
                else
                    steerPoints.Add(new SteerPoint(link.Node, 15, $"{link.Edge.LinkName}"));

                link = link.Next;
            }

            // todo: remove last point if it takes us past the 'pushback point'
            // todo: how does this all work with freaky pushback points?
            // todo: tie downs

            steerPoints.Add(new SteerPoint(Parking.PushBackLatitude, Parking.PushBackLongitude, 5, Parking.Name));
            steerPoints.Add(new ParkingPoint(Parking.Latitude, Parking.Longitude, 5, Parking.Name, Parking.Bearing, true));

            RouteProcessor.Smooth(steerPoints);
            RouteProcessor.ProcessRunwayOperations(steerPoints);

            if (MaxInPoints < steerPoints.Count)
                MaxInPoints = steerPoints.Count;

            return steerPoints;
        }
    }
}

