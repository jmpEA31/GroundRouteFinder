using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder.AptDat
{
    public class Airport
    {
        private Dictionary<ulong, TaxiNode> _taxiNodes;
        private List<Parking> _parkings; /* could be gate, helo, tie down, ... but 'parking' improved readability of some of the code */
        private List<Runway> _runways;
        private Dictionary<string, RunwayEdges> _runwayEdges;
        private List<TaxiEdge> _edges;

        private static char[] _splitters = { ' ' };

        private Dictionary<Parking, Dictionary<TaxiNode, ResultRoute>> _resultCache;

        public Airport()
        {
        }

        public void Load(string name)
        {
            _taxiNodes = new Dictionary<ulong, TaxiNode>();
            _parkings = new List<Parking>();
            _runways = new List<Runway>();
            _runwayEdges = new Dictionary<string, RunwayEdges>();
            _edges = new List<TaxiEdge>();

            string[] lines = File.ReadAllLines(name);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("100 "))
                {
                    readAirportRecord(lines[i]);
                }
                else if (lines[i].StartsWith("1201 "))
                {
                    readTaxiNode(lines[i]);
                }
                else if (lines[i].StartsWith("1202 "))
                {
                    readTaxiEdge(lines[i]);
                }
                else if (lines[i].StartsWith("1204 "))
                {
                    readTaxiEdgeOperations(lines[i]);
                }
                else if (lines[i].StartsWith("1300 "))
                {
                    readStartPoint(lines[i]);
                }
                else if (lines[i].StartsWith("1301 "))
                {
                    string[] tokens = lines[i].Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
                    _parkings.Last().SetLimits((int)(tokens[1][0] - 'A'), tokens[2]);
                }
            }

            preprocess();
        }

        private class ExitData
        {
            public TaxiNode RunwayNode;
            public TaxiNode ExitNode;
            public double Distance;
            public double ExitAngle;
        }

        private void preprocess()
        {
            // Filter out nodes with links (probably nodes for the vehicle network)
            _taxiNodes = _taxiNodes.Values.Where(v => v.IncomingNodes.Count > 0).ToDictionary(v => v.Id);

            // With unneeded nodes gone, parse the lat/lon string and convert the values to radians
            foreach (TaxiNode v in _taxiNodes.Values)
            {
                v.ComputeLonLat();
            }

            // Compute the lengths of each link (in arbitrary units
            // to avoid sin/cos and a find multiplications)
            foreach (TaxiNode v in _taxiNodes.Values)
            {
                v.ComputeDistances();
            }

            // Damn you X plane for not requiring parking spots/gate to be linked
            // to the taxi route network. Why???????
            foreach (Parking sp in _parkings)
            {
                sp.DetermineTaxiOutLocation(_taxiNodes.Values);
            }

            // Find taxi nodes closest to runways
            foreach (Runway r in _runways)
            {
                double shortestDistance = double.MaxValue;
                double shortestDisplacedDistance = double.MaxValue;

                foreach (TaxiNode vx in _taxiNodes.Values.Where(v => v.IsRunwayNode))
                {
                    double d = VortexMath.DistancePyth(vx.Latitude, vx.Longitude, r.DisplacedLatitude, r.DisplacedLongitude);
                    if (d < shortestDisplacedDistance)
                    {
                        shortestDisplacedDistance = d;
                        r.DisplacedNode = vx;
                    }

                    d = VortexMath.DistancePyth(vx.Latitude, vx.Longitude, r.Latitude, r.Longitude);
                    if (d < shortestDistance)
                    {
                        shortestDistance = d;
                        r.NearestVertex = vx;
                    }
                }

                r.Length = VortexMath.DistanceKM(r.DisplacedLatitude, r.DisplacedLongitude, r.OppositeEnd.DisplacedLatitude, r.OppositeEnd.DisplacedLongitude);
            }

            // Find taxi links that are the actual runways
            //  Then find applicable nodes for entering the runway and find the nodes off the runway connected to those
            foreach (Runway r in _runways)
            {
                var edgesKeys = _runwayEdges.Where(rwe => rwe.Value.HasVertex(r.NearestVertex.Id)).Select(rwe => rwe.Key);

                string edgeKey = edgesKeys.FirstOrDefault();
                if (!string.IsNullOrEmpty(edgeKey))
                {
                    RunwayEdges rwe = _runwayEdges[edgeKey];
                    string chain;
                    List<TaxiNode> nodes = rwe.FindChainFrom(r.NearestVertex.Id, out chain);

                    int selectedNodes = 0;
                    bool displacedNodeFound = false;

                    // Look for entries into taxinodes along the runway. We want to select all possible entries from
                    // the first two nodes with at least 1 entry
                    foreach (TaxiNode node in nodes)
                    {
                        if (selectedNodes > 1)
                            break;

                        // If the take off spot has been displaced, do not select entries at the start of the runway
                        // todo: start with these, but keep search and replace them if better ones are found.
                        if (!displacedNodeFound)
                        {
                            if (node == r.DisplacedNode)
                                displacedNodeFound = true;
                            else if (VortexMath.DistanceKM(node.Latitude, node.Longitude, r.DisplacedLatitude, r.DisplacedLongitude) < 0.150)
                                displacedNodeFound = true;
                            else
                                continue;
                        }

                        RunwayTakeOffSpot takeOffSpot = new RunwayTakeOffSpot();
                        takeOffSpot.TakeOffNode = node;


                        displacedNodeFound = true;

                        // Now inspect the current node
                        bool selectedOne = false;
                        foreach (MeasuredNode mn in node.IncomingNodes)
                        {
                            if (mn.IsRunway)
                                continue;

                            double entryAngle = VortexMath.AbsTurnAngle(mn.Bearing, r.Bearing);
                            if (entryAngle <= 0.6 * VortexMath.PI) // allow a turn of roughly 100 degrees, todo: maybe lower this?
                            {
                                selectedOne = true;
                                takeOffSpot.EntryPoints.Add(mn.SourceNode);
                            }
                        }

                        // If the node had a good entry, mark '1 node' as found
                        if (selectedOne)
                        {
                            selectedNodes++;
                            r.TakeOffSpots.Add(takeOffSpot);
                        }
                    }

                    // Do it again for exits
                    nodes.Reverse();
                    selectedNodes = 0;

                    List<ExitData> leftExits = new List<ExitData>();
                    List<ExitData> rightExits = new List<ExitData>();

                    foreach (TaxiNode node in nodes)
                    {
                        // Find nodes that have the current runway node in an incoming edge
                        IEnumerable<TaxiEdge> exitEdges = _edges.Where(edge => edge.Node1 == node.Id);
                        exitEdges = exitEdges.Where(ee => !nodes.Select(n => n.Id).Contains(ee.Node2));

                        if (exitEdges.Count() == 0)
                        {
                            //Console.WriteLine($" No links exiting out of {node.Id} @{VortexMath.DistanceKM(r.Latitude, r.Longitude, node.Latitude, node.Longitude):0.00}km");
                        }

                        foreach (TaxiEdge exit in exitEdges)
                        {
                            TaxiNode exitNode = _taxiNodes[exit.Node2];
                            MeasuredNode mn = exitNode.IncomingNodes.SingleOrDefault(inc => inc.SourceNode.Id == node.Id);
                            if (mn == null)
                                continue;

                            double exitAngle = VortexMath.TurnAngle(r.Bearing, mn.Bearing); // sign indicates left or right turn
                            if (Math.Abs(exitAngle) < 1.1 * VortexMath.PI05)
                            {
                                if (exitAngle < 0)
                                {
                                    leftExits.Add(new ExitData() { RunwayNode = node, ExitNode = exitNode, ExitAngle = exitAngle, Distance = VortexMath.DistanceKM(r.Latitude, r.Longitude, node.Latitude, node.Longitude) });
                                }
                                else
                                {
                                    rightExits.Add(new ExitData() { RunwayNode = node, ExitNode = exitNode, ExitAngle = exitAngle, Distance = VortexMath.DistanceKM(r.Latitude, r.Longitude, node.Latitude, node.Longitude) });
                                }
                                //Console.WriteLine($"{r.Number} Leaving from {node.Id} to {exitNode.Id} at"+
                                //    $" {exitAngle * VortexMath.Rad2Deg:0.0} degrees, @{VortexMath.DistanceKM(r.Latitude, r.Longitude, node.Latitude, node.Longitude):0.00}km");
                            }
                        }
                    }

                    dumpExits(r, leftExits);
                    dumpExits(r, rightExits);
                }

                Console.WriteLine("-----------------------------");
            }

            foreach (var kvp in _runwayEdges)
            {
                kvp.Value.Process();
            }
        }

        private void dumpExits(Runway r, List<ExitData> exits)
        {
            int selected = 0;
            ExitData selected1 = null;
            ExitData selected2 = null;
            ExitData selected3 = null;
            ExitData selected4 = null;
            foreach (ExitData ed in exits)
            {
                if (ed.ExitAngle < (100.0 * VortexMath.Deg2Rad) && ed.Distance >= (r.Length * 0.4))
                {
                    selected4 = ed;
                }

                switch (selected)
                {
                    case 0:
                        if (ed.ExitAngle < (100.0 * VortexMath.Deg2Rad) && ed.Distance >= (r.Length * 0.6))
                        {
                            selected1 = ed;
                            selected++;
                        }
                        break;
                    case 1:
                        if (ed.ExitAngle < (46.0 * VortexMath.Deg2Rad) && ed.Distance >= (r.Length * 0.4))
                        {
                            selected2 = ed;
                            selected++;
                        }
                        break;
                    case 2:
                        if (ed.ExitAngle < (46.0 * VortexMath.Deg2Rad) && ed.Distance >= (r.Length * 0.4) && (selected2.Distance - ed.Distance) > 0.3)
                        {
                            selected3 = ed;
                            selected++;
                        }

                        break;
                    default:
                        break;
                }
            }
            dumpExit(r, selected1, "max range");
            dumpExit(r, selected2, "first alternate");
            dumpExit(r, selected3, "second alternate");
            dumpExit(r, selected4, "minimum");
        }



        private void dumpExit(Runway r, ExitData exit, string note)
        {
            if (exit != null)
            {
                Console.WriteLine($"{r.Number} Leaving from {exit.RunwayNode.Id} to {exit.ExitNode.Id} at {exit.ExitAngle * VortexMath.Rad2Deg:0.0} degrees, @{exit.Distance:0.00}km {note}");
            }
            else
            {
                Console.WriteLine($"{r.Number}: None for {note}");
            }
        }

        public void FindOutboundRoutes()
        {
            findOutboundRoutes(_taxiNodes.Values, _parkings);
        }

        private void findOutboundRoutes(IEnumerable<TaxiNode> nodes, IEnumerable<Parking> parkings)
        {
            // for each runway
            foreach (Runway runway in _runways)
            {
                _resultCache = new Dictionary<Parking, Dictionary<TaxiNode, ResultRoute>>();

                // for each takeoff spot
                foreach (RunwayTakeOffSpot takeoffSpot in runway.TakeOffSpots)
                {
                    // for each size
                    for (int size = TaxiNode.Sizes - 1; size >= 0; size--)
                    {
                        // find shortest path from each parking to each takeoff spot considering each entrypoint
                        foreach (TaxiNode runwayEntryNode in takeoffSpot.EntryPoints)
                        {
                            findShortestPaths(nodes, parkings, runwayEntryNode, size);
                            foreach (Parking parking in parkings)
                            {
                                ResultRoute bestResultSoFar = getBestResultSoFar(runwayEntryNode, parking, size);
                                if (bestResultSoFar.Distance > parking.NearestVertex.DistanceToTarget)
                                {
                                    ResultRoute better = extractRoute(parking.NearestVertex, size);
                                    better.TakeoffSpot = takeoffSpot;
                                    improveResult(runwayEntryNode, parking, size, bestResultSoFar, better);
                                }
                            }
                        }
                    }
                }

                // Write Results
                foreach (KeyValuePair<Parking, Dictionary<TaxiNode, ResultRoute>> kvp in _resultCache)
                {
                    int currentEntry = 1;

                    IEnumerable<KeyValuePair<TaxiNode, ResultRoute>> best2 = kvp.Value.OrderBy(v => v.Value.Distance).Take(2);
                    foreach (KeyValuePair<TaxiNode, ResultRoute> kvpi in best2)
                    {
                        writeOutboundRoutes(runway, kvpi.Value.TakeoffSpot, currentEntry++, nodes, kvp.Key, kvpi.Value);
                    }
                }
            }
        }

        private void writeOutboundRoutes(Runway runway, RunwayTakeOffSpot takeoffSpot, int entry, IEnumerable<TaxiNode> nodes, Parking parking, ResultRoute results)
        {
            ResultRoute route = results;
            while (route != null)
            {
                if (route.Distance == double.MaxValue)
                    continue;

                LinkedNode link = route.RouteStart;
                TaxiNode nodeToWrite = route.NearestNode;

                // Map the XP 
                List<int> routeSizes = new List<int>();
                foreach (int size in route.ValidForSizes)
                {
                    // Try to skip routes for planes larger than the parking spot allows
                    if (parking.MaxSize < size)
                        continue;

                    switch (size)
                    {
                        case 0: // XPlane type A 'wingspan < 15'
                            routeSizes.AddRange(new int[] { 0, 7, 8, 9 }); // Fighter, Light Jet, Light Prop, Helicopter
                            break;
                        case 1: // XPlane type B 'wingspan < 24'
                            routeSizes.AddRange(new int[] { 5, 6 }); // Medium Jet, Medium Prop
                            break;
                        case 2: // XPlane type C 'wingspan < 36'
                            routeSizes.Add(3); // Large Jet
                            break;
                        case 3: // XPlane type D 'wingspan < 52'
                            routeSizes.Add(4); // Large Prop
                            break;
                        case 4: // XPlane type E 'wingspan < 65'
                            routeSizes.Add(2); // Heavy Jet
                            break;
                        case 5: // XPlane type F 'wingspan < 80'
                        default:
                            routeSizes.Add(1); // Supah Heavy Jet
                            break;
                    }
                }

                // If route does not apply to any size anymore, skip it
                if (routeSizes.Count == 0)
                    continue;

                int speed = 15;

                string allSizes = string.Join(" ", routeSizes.OrderBy(w => w));

                string sizeName = (routeSizes.Count == 10) ? "all" : allSizes.Replace(" ", "");
                string fileName = $"E:\\GroundRoutes\\Departures\\LFPG\\{parking.FileNameSafeName}_to_{runway.Number}-{entry}_{sizeName}.txt";
                File.Delete(fileName);
                using (StreamWriter sw = File.CreateText(fileName))
                {
                    sw.Write($"STARTAIRCRAFTTYPE\n{allSizes}\nENDAIRCRAFTTYPE\n\n");
                    sw.Write("STARTCARGO\n0\nENDCARGO\n\n");
                    sw.Write("STARTMILITARY\n0\nENDMILITARY\n\n");
                    sw.Write($"STARTRUNWAY\n{runway.Number}\nENDRUNWAY\n\n");
                    sw.Write("START_PARKING_CENTER\nNOSEWHEEL\nEND_PARKING_CENTER\n\n");
                    sw.Write("STARTSTEERPOINTS\n");

                    // Write the start point
                    sw.Write($"{parking.Latitude * VortexMath.Rad2Deg} {parking.Longitude * VortexMath.Rad2Deg} -2 {parking.Bearing * VortexMath.Rad2Deg:0} 0 0 {parking.Name}\n");

                    // Write Pushback node, allowing room for turn
                    double addLat = 0;
                    double addLon = 0;

                    // See if we need to skip the first route node
                    if (parking.AlternateAfterPushBack != null && parking.AlternateAfterPushBack == route.RouteStart.Node)
                    {
                        // Our pushback point is better than the first point of the route
                        nodeToWrite = parking.AlternateAfterPushBack;
                    }
                    else if (VortexMath.DistancePyth(parking.PushBackLatitude, parking.PushBackLongitude, route.NearestNode.Latitude, route.NearestNode.Longitude) < 0.000000001)
                    {
                        // pushback node is the first route point
                        nodeToWrite = route.NearestNode;
                    }

                    // insert one more point here where the plane is pushed a little bit away from the next point
                    if (nodeToWrite != null)
                    {
                        double nextPushBearing = VortexMath.BearingRadians(nodeToWrite.Latitude, nodeToWrite.Longitude, parking.PushBackLatitude, parking.PushBackLongitude);
                        double turn = VortexMath.AbsTurnAngle(parking.Bearing, nextPushBearing);
                        double distance = 0.040 * ((VortexMath.PI - turn) / VortexMath.PI);
                        VortexMath.PointFrom(parking.PushBackLatitude, parking.PushBackLongitude, parking.Bearing, distance, ref addLat, ref addLon);
                        sw.Write($"{addLat * VortexMath.Rad2Deg:0.00000000} {addLon * VortexMath.Rad2Deg:0.00000000} -1 -1 0 0 {link.LinkName}\n");
                        VortexMath.PointFrom(parking.PushBackLatitude, parking.PushBackLongitude, nextPushBearing, 0.030, ref addLat, ref addLon);
                        sw.Write($"{addLat * VortexMath.Rad2Deg:0.00000000} {addLon * VortexMath.Rad2Deg:0.00000000} 10 -1 0 0 {link.LinkName}\n");
                    }

                    bool wasOnRunway = false;

                    double lastBearing = VortexMath.BearingRadians(parking.PushBackLatitude, parking.PushBackLongitude, nodeToWrite.Latitude, nodeToWrite.Longitude);
                    double lastLatitude = nodeToWrite.Latitude;
                    double lastLongitude = nodeToWrite.Longitude;

                    while (link.Node != null)
                    {
                        bool smoothed = false;

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

                        // todo: proper speed smoothing etc
                        if (link.Next?.Next?.Next == null)
                        {
                            speed = 10;
                        }
                        else if (link.Next?.Next == null)
                        {
                            speed = 8;
                        }


                        // Check for corners that need to be smoothed
                        if (link.Next.Node != null)
                        {
                            double turnAngle = 0;
                            double nextBearing = VortexMath.BearingRadians(link.Node.Latitude, link.Node.Longitude, link.Next.Node.Latitude, link.Next.Node.Longitude);
                            turnAngle = VortexMath.AbsTurnAngle(lastBearing, nextBearing);
                            if (turnAngle > VortexMath.PI025)
                            {
                                double availableDistance = VortexMath.DistanceKM(lastLatitude, lastLongitude, link.Node.Latitude, link.Node.Longitude);
                                if (availableDistance > 0.025)
                                {
                                    VortexMath.PointFrom(link.Node.Latitude, link.Node.Longitude, lastBearing + VortexMath.PI, 0.025, ref addLat, ref addLon);
                                    writeNode(sw, addLat, addLon, 8, linkOperation);
                                }

                                availableDistance = VortexMath.DistanceKM(link.Node.Latitude, link.Node.Longitude, link.Next.Node.Latitude, link.Next.Node.Longitude);
                                if (availableDistance > 0.025)
                                {
                                    VortexMath.PointFrom(link.Node.Latitude, link.Node.Longitude, nextBearing, 0.025, ref addLat, ref addLon);
                                    writeNode(sw, addLat, addLon, speed, linkOperation2);
                                }
                                smoothed = true;
                            }
                            lastBearing = nextBearing;
                        }

                        if (!smoothed)
                        {
                            writeNode(sw, link.Node.LatitudeString, link.Node.LongitudeString, speed, $"-1 {linkOperation}");
                        }

                        lastLatitude = link.Node.Latitude;
                        lastLongitude = link.Node.Longitude;
                        link = link.Next;
                    }
                    writeNode(sw, takeoffSpot.TakeOffNode.LatitudeString, takeoffSpot.TakeOffNode.LongitudeString, 6, $"-1 {runway.Number} 2");

                    VortexMath.PointFrom(takeoffSpot.TakeOffNode.Latitude, takeoffSpot.TakeOffNode.Longitude, runway.Bearing, 0.022, ref addLat, ref addLon);
                    writeNode(sw, addLat, addLon, 6, $"-1 {runway.Number} 2");

                    sw.Write("ENDSTEERPOINTS\n");
                }
                route = route.NextSizes;
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


        private ResultRoute getBestResultSoFar(TaxiNode runwayEntryNode, Parking parking, int size)
        {
            if (!_resultCache.ContainsKey(parking))
                _resultCache.Add(parking, new Dictionary<TaxiNode, ResultRoute>());

            if (!_resultCache[parking].ContainsKey(runwayEntryNode))
                _resultCache[parking].Add(runwayEntryNode, new ResultRoute(size));

            return _resultCache[parking][runwayEntryNode].RouteForSize(size);
        }


        private void improveResult(TaxiNode runwayEntryNode, Parking parking, int size, ResultRoute bestResultSoFar, ResultRoute better)
        {
            bestResultSoFar.ImproveResult(better);
        }

        private ResultRoute extractRoute(TaxiNode nearestVertex, int size)
        {
            ResultRoute extracted = new ResultRoute(size);
            extracted.NearestNode = nearestVertex;
            ulong node1 = extracted.NearestNode.Id;
            extracted.Distance = nearestVertex.DistanceToTarget;
            extracted.RouteStart = new LinkedNode() { Node = nearestVertex.PathToTarget, Next = null, LinkName = nearestVertex.NameToTarget };
            LinkedNode currentLink = extracted.RouteStart;
            TaxiNode pathNode = nearestVertex.PathToTarget;

            while (pathNode != null)
            {
                ulong node2 = pathNode.Id;
                TaxiEdge edge = _edges.Single(e => e.Node1 == node1 && e.Node2 == node2 || e.Node1 == node2 && e.Node2 == node1);
                currentLink.Next = new LinkedNode()
                {
                    Node = pathNode.PathToTarget,
                    Next = null,
                    LinkName = pathNode.NameToTarget,
                    ActiveZone = (edge != null) ? edge.ActiveZone : false,
                    ActiveFor = (edge != null) ? edge.ActiveFor : "?",
                };
                node1 = node2;
                currentLink = currentLink.Next;
                pathNode = pathNode.PathToTarget;
            }

            return extracted;
        }


        private void findShortestPaths(IEnumerable<TaxiNode> nodes, IEnumerable<Parking> startPoints, TaxiNode targetNode, int size)
        {
            List<TaxiNode> untouchedNodes = nodes.ToList();
            List<TaxiNode> touchedNodes = new List<TaxiNode>();

            foreach (TaxiNode node in nodes)
            {
                node.DistanceToTarget = double.MaxValue;
                node.PathToTarget = null;
            }

            // Setup the targetnode
            targetNode.DistanceToTarget = 0;
            targetNode.PathToTarget = null;

            foreach (MeasuredNode vto in targetNode.IncomingNodes)
            {
                if (size > vto.MaxSize)
                    continue;

                if (untouchedNodes.Contains(vto.SourceNode) && !touchedNodes.Contains(vto.SourceNode))
                {
                    untouchedNodes.Remove(vto.SourceNode);
                    touchedNodes.Add(vto.SourceNode);
                }

                vto.SourceNode.DistanceToTarget = vto.RelativeDistance;
                vto.SourceNode.PathToTarget = targetNode;
                vto.SourceNode.NameToTarget = vto.LinkName;
                vto.SourceNode.PathIsRunway = vto.IsRunway;
            }

            //doneNodes.Add(targetNode);
            untouchedNodes.Remove(targetNode);

            // and branch out from there
            while (touchedNodes.Count() > 0)
            {
                double min = touchedNodes.Min(a => a.DistanceToTarget);
                targetNode = touchedNodes.FirstOrDefault(a => a.DistanceToTarget == min);

                foreach (MeasuredNode vto in targetNode.IncomingNodes)
                {
                    if (size > vto.MaxSize)
                        continue;

                    // Try to force smaller aircraft to take their specific routes
                    double penalizedDistance = 0; // vto.RelativeDistance * (1.0 + 2.0 * (vto.MaxSize - size));

                    if (vto.IsRunway)
                        penalizedDistance += 1.0;

                    if (vto.SourceNode.DistanceToTarget > (targetNode.DistanceToTarget + penalizedDistance))
                    {
                        if (untouchedNodes.Contains(vto.SourceNode) && !touchedNodes.Contains(vto.SourceNode))
                        {
                            untouchedNodes.Remove(vto.SourceNode);
                            touchedNodes.Add(vto.SourceNode);
                        }

                        vto.SourceNode.DistanceToTarget = (targetNode.DistanceToTarget + penalizedDistance);
                        vto.SourceNode.PathToTarget = targetNode;
                        vto.SourceNode.NameToTarget = vto.LinkName;
                        vto.SourceNode.PathIsRunway = vto.IsRunway;
                    }
                }

                touchedNodes.Remove(targetNode);
            }
        }




        private void readAirportRecord(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
            Runway r1 = new Runway();
            r1.Number = tokens[8];
            r1.Latitude = double.Parse(tokens[9]) * VortexMath.Deg2Rad;
            r1.Longitude = double.Parse(tokens[10]) * VortexMath.Deg2Rad;
            r1.Displacement = double.Parse(tokens[11]) / 1000.0; // Use KM for distance
            _runways.Add(r1);

            Runway r2 = new Runway();
            r2.Number = tokens[17];
            r2.Latitude = double.Parse(tokens[18]) * VortexMath.Deg2Rad;
            r2.Longitude = double.Parse(tokens[19]) * VortexMath.Deg2Rad;
            r2.Displacement = double.Parse(tokens[20]) / 1000.0; // Use KM for distance
            _runways.Add(r2);

            r1.OppositeEnd = r2;
            r2.OppositeEnd = r1;
        }

        private void readTaxiNode(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
            ulong id = ulong.Parse(tokens[4]);
            _taxiNodes[id] = new TaxiNode(id, tokens[1], tokens[2]);
            _taxiNodes[id].Name = string.Join(" ", tokens.Skip(5));
        }

        private void readTaxiEdge(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
            ulong va = ulong.Parse(tokens[1]);
            ulong vb = ulong.Parse(tokens[2]);
            _edges.Add(new TaxiEdge() { Node1 = va, Node2 = vb, ActiveZone = false });

            // todo: move the next stuff to pre proc

            int maxSize = 5;
            bool isRunway = (tokens[4][0] != 't');
            if (!isRunway)
                maxSize = (int)(tokens[4][8] - 'A');
            else
            {
                if (!_runwayEdges.ContainsKey(tokens[5]))
                {
                    _runwayEdges[tokens[5]] = new RunwayEdges();
                }

                _runwayEdges[tokens[5]].AddEdge(_taxiNodes[va], _taxiNodes[vb]);
            }

            string linkName = tokens.Length > 5 ? string.Join(" ", tokens.Skip(5)) : "";
            _taxiNodes[vb].AddEdgeFrom(_taxiNodes[va], maxSize, isRunway, linkName);
            if (tokens[3][0] == 't')
            {
                _taxiNodes[va].AddEdgeFrom(_taxiNodes[vb], maxSize, isRunway, linkName);
            }
        }

        private void readTaxiEdgeOperations(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
            _edges.Last().ActiveZone = true;
            string[] rwys = tokens[2].Split(',');
            _edges.Last().ActiveFor = rwys[0];
        }

        private void readStartPoint(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
            if (tokens[5] != "helos") // ignore helos for now
            {
                Parking sp = new Parking();
                sp.Latitude = double.Parse(tokens[1]) * VortexMath.Deg2Rad;
                sp.Longitude = double.Parse(tokens[2]) * VortexMath.Deg2Rad;
                sp.Bearing = ((double.Parse(tokens[3]) + 540) * VortexMath.Deg2Rad) % (VortexMath.PI2) - Math.PI;
                sp.Type = tokens[4];
                sp.Jets = tokens[5];
                sp.Name = string.Join(" ", tokens.Skip(6));
                _parkings.Add(sp);
            }
        }
    }
}
