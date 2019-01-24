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

        private Dictionary<TaxiNode, Dictionary<int, ResultRoute>> _results;

        public InboundResults(Parking parking)
        {
            Parking = parking;
            _results = new Dictionary<TaxiNode, Dictionary<int, ResultRoute>>();
        }

        public void AddResult(IEnumerable<TaxiEdge> edges, Runway r, TaxiNode runwayNode, TaxiNode origin, int maxSizeCurrentResult)
        {
            if (!_results.ContainsKey(runwayNode))
                _results[runwayNode] = new Dictionary<int, ResultRoute>();

            Dictionary<int, ResultRoute> originResults = _results[runwayNode];

            if (originResults.Count == 0)
            {
                originResults.Add(maxSizeCurrentResult, ResultRoute.ExtractRoute(edges, r, origin, maxSizeCurrentResult));
            }
            else
            {
                int minSize = originResults.Min(or => or.Key);
                if (originResults[minSize].Distance > origin.DistanceToTarget)
                {
                    if (minSize > maxSizeCurrentResult)
                    {
                        originResults.Add(maxSizeCurrentResult, ResultRoute.ExtractRoute(edges, r, origin, maxSizeCurrentResult));
                        originResults[minSize].MinSize = (maxSizeCurrentResult + 1);
                    }
                    else if (minSize == maxSizeCurrentResult)
                    {
                        originResults[minSize] = ResultRoute.ExtractRoute(edges, r, origin, maxSizeCurrentResult);
                    }
                }
            }
        }

        public void WriteRoutes()
        {
            foreach (KeyValuePair<TaxiNode, Dictionary<int, ResultRoute>> sizeRoutes in _results)
            {
                for (int size = Parking.MaxSize; size >= 0; size--)
                {
                    if (sizeRoutes.Value.ContainsKey(size))
                    {
                        ResultRoute route = sizeRoutes.Value[size];
                        if (route.MinSize > Parking.MaxSize)
                            continue;

                        List<int> routeSizes = new List<int>();
                        for (int s = route.MinSize; s<=route.MaxSize; s++)
                        {
                            routeSizes.AddRange(Settings.XPlaneCategoryToWTType(s));
                        }

                        string allSizes = string.Join(" ", routeSizes.OrderBy(w => w));

                        string sizeName = (routeSizes.Count == 10) ? "all" : allSizes.Replace(" ", "");
                        string fileName = $"E:\\GroundRoutes\\Arrival\\LFPG\\{route.Runway.Designator}_to_{Parking.FileNameSafeName}-{sizeRoutes.Key.Id}_{sizeName}.txt";
                        File.Delete(fileName);
                        using (StreamWriter sw = File.CreateText(fileName))
                        {
                            sw.Write($"STARTAIRCRAFTTYPE\n{allSizes}\nENDAIRCRAFTTYPE\n\n");
                            sw.Write("START_PARKING_CENTER\nNOSEWHEEL\nEND_PARKING_CENTER\n\n");
                            sw.Write($"STARTRUNWAY\n{route.Runway.Designator}\nENDRUNWAY\n\n");
                            sw.Write("STARTSTEERPOINTS\n");

                            List<SteerPoint> steerPoints = new List<SteerPoint>();

                            // Add the start point (this one or displaced???)
                            RunwayPoint threshold = new RunwayPoint(route.Runway.Latitude, route.Runway.Longitude, 55, $"{route.Runway.Designator} Threshold", route.RouteStart.ActiveFor);
                            threshold.OnRunway = true;
                            threshold.IsExiting = true;
                            steerPoints.Add(threshold);

                            foreach (TaxiNode node in route.Runway.RunwayNodes)
                            {
                                steerPoints.Add(new RunwayPoint(node.Latitude, node.Longitude, 55, $"{route.Runway.Designator}", route.RouteStart.ActiveFor));

                                if (node == sizeRoutes.Key)
                                    break;
                            }

                            steerPoints.Add(new RunwayPoint(route.NearestNode.Latitude, route.NearestNode.Longitude, 20, route.RouteStart.LinkName, route.RouteStart.ActiveFor));

                            LinkedNode link = route.RouteStart;
                            bool wasOnRunway = true;
                            while (link.Node != null)
                            {
                                string linkOperation = "";
                                string linkOperation2 = "";
                                if (link.ActiveZone)
                                {
                                    if (!wasOnRunway)
                                    {
                                        linkOperation = $"-1 {link.ActiveFor} 1";
                                        linkOperation2 = $"-1 {link.ActiveFor} 2";
                                        wasOnRunway = true;
                                    }
                                    else
                                    {
                                        linkOperation = $"-1 {link.ActiveFor} 2";
                                        linkOperation2 = linkOperation;
                                    }
                                }
                                else
                                {
                                    wasOnRunway = false;
                                    linkOperation = $"-1 0 0 {link.LinkName}";
                                    linkOperation2 = linkOperation;
                                }

                                if (link.ActiveZone)
                                    steerPoints.Add(new RunwayPoint(link.Node.Latitude, link.Node.Longitude, 15, $"{link.LinkName}", $"{link.ActiveFor}"));
                                else
                                    steerPoints.Add(new SteerPoint(link.Node.Latitude, link.Node.Longitude, 15, $"{link.LinkName}"));

                                link = link.Next;
                            }

                            steerPoints.Add(new SteerPoint(Parking.PushBackLatitude, Parking.PushBackLongitude, 5, Parking.Name));
                            steerPoints.Add(new ParkingPoint(Parking.Latitude, Parking.Longitude, 5, Parking.Name, Parking.Bearing));

                            RouteProcessor.Smooth(steerPoints);
                            RouteProcessor.ProcessRunwayOperations(steerPoints);
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

        private void writeNode(StreamWriter sw, double latitude, double longitude, int speed, string tail)
        {
            sw.Write($"{latitude * VortexMath.Rad2Deg:0.00000000} {longitude * VortexMath.Rad2Deg:0.00000000} {speed} {tail}\n");
        }

        private void writeNode(StreamWriter sw, string latitude, string longitude, int speed, string tail)
        {
            sw.Write($"{latitude} {longitude} {speed} {tail}\n");
        }

    }
}
