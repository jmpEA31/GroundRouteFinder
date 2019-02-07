using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GroundRouteFinder.AptDat;
using GroundRouteFinder.Output;

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

        public void WriteRoutes(string outputPath, bool kml)
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
                        using (RouteWriter sw = RouteWriter.Create(kml ? 0 : 1, fileName, allSizes, 0, 0, route.Runway.Designator, "NOSEWHEEL"))
                        {
                            IEnumerable<SteerPoint> steerPoints = buildSteerPoints(route, sizeRoutes.Key);
                            foreach (SteerPoint steerPoint in steerPoints)
                            {
                                sw.Write(steerPoint);
                            }
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

