using System;
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
    public class InboundResults
    {
        public Parking Parking;

        private readonly IEnumerable<TaxiEdge> _edges;
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

        public int WriteRoutes(string outputPath, bool kml)
        {
            int count = 0;

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
                            Logger.Log($"WARN {Parking.Name} (Max)Cat {Parking.MaxSize} Types: {string.Join(" ", Parking.XpTypes)} does not map to any WT types.");
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

                        IEnumerable<SteerPoint> steerPoints = BuildSteerPoints(route, sizeRoutes.Key);
                        if (steerPoints.Count() <= Settings.MaxSteerpoints)
                        {
                            string allSizes = string.Join(" ", wtTypes.Select(w => (int)w).OrderBy(w => w));
                            string sizeName = (wtTypes.Count() == 10) ? "all" : allSizes.Replace(" ", "");
                            string fileName = Path.Combine(outputPath, $"{route.Runway.Designator}_to_{Parking.FileNameSafeName}_{route.AvailableRunwayLength * VortexMath.KmToFoot:00000}_{sizeName}");

                            using (RouteWriter sw = RouteWriter.Create(kml ? 0 : 1, fileName, allSizes, -1, -1, route.Runway.Designator, ParkingReferenceConverter.ParkingReference(Settings.ParkingReference)))
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
                            Logger.Log($"Route from <{route.Runway.Designator}> to {Parking.FileNameSafeName} not written. Too many steerpoints ({steerPoints.Count()} vs {Settings.MaxSteerpoints})");
                        }
                    }
                }
            }
            return count;
        }

        private IEnumerable<SteerPoint> BuildSteerPoints(ResultRoute route, TaxiNode runwayExitNode)
        {
            List<SteerPoint> steerPoints = new List<SteerPoint>();

            // Route should start at the (displaced) threshold
            RunwayPoint threshold = new RunwayPoint(route.Runway.DisplacedNode, 55, $"{route.Runway.Designator} Threshold", route.RouteStart.Edge.ActiveForRunway(route.Runway.Designator))
            {
                OnRunway = true,
                IsExiting = true
            };
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
                bool activeZone = false;
                string activeFor = "";

                if (link.Edge.ActiveZone)
                {
                    activeZone = true;
                    activeFor = link.Edge.ActiveForRunway("");
                }
                else if (link.Next.Edge != null && link.Next.Edge.ActiveZone)
                {
                    activeZone = true;
                    activeFor = link.Next.Edge.ActiveForRunway("");
                }

                if (activeZone)
                    steerPoints.Add(new RunwayPoint(link.Node.Latitude, link.Node.Longitude, 15, $"{link.Edge.LinkName}", activeFor));
                else
                    steerPoints.Add(new SteerPoint(link.Node.Latitude, link.Node.Longitude, 15, $"{link.Edge.LinkName}"));

                link = link.Next;
            }

            // remove last point if it takes us past the 'pushback point'
            if (steerPoints.Count > 1)
            {
                SteerPoint oneButLast = steerPoints.ElementAt(steerPoints.Count - 2);
                SteerPoint last = steerPoints.ElementAt(steerPoints.Count - 1);
                double lastBearing = VortexMath.BearingRadians(oneButLast, last);
                double bearingToPush = VortexMath.BearingRadians(last.Latitude, last.Longitude, Parking.PushBackLatitude, Parking.PushBackLongitude);
                double turnToPush = VortexMath.AbsTurnAngle(lastBearing, bearingToPush);
                if (turnToPush > VortexMath.Deg100Rad)
                {
                    steerPoints.RemoveAt(steerPoints.Count - 1);
                }
            }

            // todo: how does this all work with freaky pushback points?
            // todo: tie downs

            steerPoints.Add(new SteerPoint(Parking.PushBackLatitude, Parking.PushBackLongitude, 5, Parking.Name));
            steerPoints.Add(new ParkingPoint(Parking.Latitude, Parking.Longitude, 5, Parking.Name, Parking.Bearing, true));

            //RouteProcessor.Smooth(steerPoints);
            RouteProcessor.ProcessRunwayOperations(steerPoints);

            if (MaxInPoints < steerPoints.Count)
                MaxInPoints = steerPoints.Count;

            return steerPoints;
        }
    }
}

