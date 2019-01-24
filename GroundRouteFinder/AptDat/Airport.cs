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
        private Dictionary<ulong, TaxiNode> _nodeDict;
        private IEnumerable<TaxiNode> _taxiNodes;
        private List<Parking> _parkings; /* could be gate, helo, tie down, ... but 'parking' improved readability of some of the code */
        private List<Runway> _runways;
//        private Dictionary<string, RunwayEdges> _runwayEdges;
        private List<TaxiEdge> _edges;

        private static char[] _splitters = { ' ' };

        private Dictionary<Parking, Dictionary<TaxiNode, ResultRoute>> _resultCache;


        public Airport()
        {
        }

        public void Load(string name)
        {
            _nodeDict = new Dictionary<ulong, TaxiNode>();
            _parkings = new List<Parking>();
            _runways = new List<Runway>();
            //_runwayEdges = new Dictionary<string, RunwayEdges>();
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

        private void preprocess()
        {
            // Filter out nodes with links (probably nodes for the vehicle network)
            _taxiNodes = _nodeDict.Values.Where(v => v.IncomingNodes.Count > 0);

            // With unneeded nodes gone, parse the lat/lon string and convert the values to radians
            foreach (TaxiNode v in _taxiNodes)
            {
                v.ComputeLonLat();
            }

            // Compute the lengths of each link (in arbitrary units
            // to avoid sin/cos and a find multiplications)
            foreach (TaxiNode v in _taxiNodes)
            {
                v.ComputeDistances();
            }

            // Damn you X plane for not requiring parking spots/gate to be linked
            // to the taxi route network. Why???????
            foreach (Parking sp in _parkings)
            {
                sp.DetermineTaxiOutLocation(_taxiNodes);
            }

            // Find taxi links that are the actual runways
            //  Then find applicable nodes for entering the runway and find the nodes off the runway connected to those
            foreach (Runway r in _runways)
            {
                r.Analyze(_taxiNodes, _edges);
                Console.WriteLine("-----------------------------");
            }
        }

        public void FindInboundRoutes()
        {
            foreach (Parking parking in _parkings)
            {
                InboundResults ir = new InboundResults(parking);
                for (int size = parking.MaxSize; size >= 0; size--)
                {
                    // Nearest node should become 'closest to computed pushback point'
                    findShortestPaths(_taxiNodes, parking.NearestNode, size);

                    //StreamWriter tnd = File.CreateText($"e:\\groundroutes\\deb-{parking.Name}.csv");
                    //tnd.WriteLine($"lat,lon,pen,id");
                    //foreach (TaxiNode tn in _taxiNodes)
                    //{
                    //    tnd.WriteLine($"{tn.Latitude*VortexMath.Rad2Deg},{tn.Longitude * VortexMath.Rad2Deg},{tn.DistanceToTarget},{tn.Id}");
                    //}
                    //tnd.Close();

                    // Pick the runway exit points for the selected size
                    foreach (Runway r in _runways)
                    {
                        foreach (Runway.RunwayNodeUsage use in Settings.SizeToUsage[size])
                        {
                            Runway.UsageNodes exitNodes = r.GetNodesForUsage(use);
                            Runway.NodeUsage usage = exitNodes.Roles[(int)Runway.UsageNodes.Role.Left];
                            double bestDistance = double.MaxValue;
                            Runway.UsageNodes.Role bestSide = Runway.UsageNodes.Role.Max;
                            if (usage != null)
                            {
                                bestDistance = usage.OffRunwayNode.DistanceToTarget;
                                bestSide = Runway.UsageNodes.Role.Left;
                            }

                            usage = exitNodes.Roles[(int)Runway.UsageNodes.Role.Right];
                            if (usage != null)
                            {
                                if (usage.OffRunwayNode.DistanceToTarget < bestDistance)
                                {
                                    bestDistance = usage.OffRunwayNode.DistanceToTarget;
                                    bestSide = Runway.UsageNodes.Role.Right;
                                }
                            }

                            if (bestSide != Runway.UsageNodes.Role.Max)
                            {
                                usage = exitNodes.Roles[(int)bestSide];
                                ir.AddResult(_edges, r, usage.OnRunwayNode, usage.OffRunwayNode, size);
                            }
                        }
                    }
                }
                ir.WriteRoutes();
            }
        }

        public void FindOutboundRoutes()
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
                            findShortestPaths(_taxiNodes, runwayEntryNode, size);
                            foreach (Parking parking in _parkings)
                            {
                                ResultRoute bestResultSoFar = getBestResultSoFar(runwayEntryNode, parking, size);
                                if (bestResultSoFar.Distance > parking.NearestNode.DistanceToTarget)
                                {
                                    ResultRoute better = extractRoute(parking.NearestNode, size);
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
                        writeOutboundRoutes(runway, kvpi.Value.TakeoffSpot, currentEntry++, _taxiNodes, kvp.Key, kvpi.Value);
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

                    routeSizes.AddRange(Settings.XPlaneCategoryToWTType(size));
                }

                // If route does not apply to any size anymore, skip it
                if (routeSizes.Count == 0)
                    continue;

                int speed = 15;

                string allSizes = string.Join(" ", routeSizes.OrderBy(w => w));

                string sizeName = (routeSizes.Count == 10) ? "all" : allSizes.Replace(" ", "");
                string fileName = $"E:\\GroundRoutes\\Departures\\LFPG\\{parking.FileNameSafeName}_to_{runway.Designator}-{entry}_{sizeName}.txt";
                File.Delete(fileName);
                using (StreamWriter sw = File.CreateText(fileName))
                {
                    sw.Write($"STARTAIRCRAFTTYPE\n{allSizes}\nENDAIRCRAFTTYPE\n\n");
                    sw.Write("STARTCARGO\n0\nENDCARGO\n\n");
                    sw.Write("STARTMILITARY\n0\nENDMILITARY\n\n");
                    sw.Write($"STARTRUNWAY\n{runway.Designator}\nENDRUNWAY\n\n");
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
                            double nextBearing = VortexMath.BearingRadians(link.Node, link.Next.Node);
                            turnAngle = VortexMath.AbsTurnAngle(lastBearing, nextBearing);
                            if (turnAngle > VortexMath.PI025)
                            {
                                double availableDistance = VortexMath.DistanceKM(lastLatitude, lastLongitude, link.Node.Latitude, link.Node.Longitude);
                                if (availableDistance > 0.025)
                                {
                                    VortexMath.PointFrom(link.Node.Latitude, link.Node.Longitude, lastBearing + VortexMath.PI, 0.025, ref addLat, ref addLon);
                                    writeNode(sw, addLat, addLon, 8, linkOperation);
                                }

                                availableDistance = VortexMath.DistanceKM(link.Node, link.Next.Node);
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
                    writeNode(sw, takeoffSpot.TakeOffNode.LatitudeString, takeoffSpot.TakeOffNode.LongitudeString, 6, $"-1 {runway.Designator} 2");

                    VortexMath.PointFrom(takeoffSpot.TakeOffNode.Latitude, takeoffSpot.TakeOffNode.Longitude, runway.Bearing, 0.022, ref addLat, ref addLon);
                    writeNode(sw, addLat, addLon, 6, $"-1 {runway.Designator} 2");

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
                TaxiEdge edge = _edges.Single(e => e.StartNodeId == node1 && e.EndNodeId == node2);
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


        private void findShortestPaths(IEnumerable<TaxiNode> nodes, TaxiNode targetNode, int size)
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
                vto.SourceNode.BearingToTarget = VortexMath.BearingRadians(vto.SourceNode, targetNode);
            }

            //doneNodes.Add(targetNode);
            untouchedNodes.Remove(targetNode);

            // and branch out from there
            while (touchedNodes.Count() > 0)
            {
                double min = touchedNodes.Min(a => a.DistanceToTarget);
                targetNode = touchedNodes.FirstOrDefault(a => a.DistanceToTarget == min);

                foreach (MeasuredNode incoming in targetNode.IncomingNodes)
                {
                    if (size > incoming.MaxSize)
                        continue;

                    // Try to force smaller aircraft to take their specific routes
                    double penalizedDistance = incoming.RelativeDistance; // vto.RelativeDistance * (1.0 + 2.0 * (vto.MaxSize - size));

                    //double bearingToTarget = VortexMath.BearingRadians(incoming.SourceNode.Latitude, incoming.SourceNode.Longitude, targetNode.Latitude, targetNode.Longitude);
                    //double turnToTarget = VortexMath.AbsTurnAngle(targetNode.BearingToTarget, incoming.Bearing);

                        //penalizedDistance += 5.0;
                    //else if (turnToTarget > VortexMath.PI033)
                    //    penalizedDistance += 0.01;


                    if (incoming.IsRunway)
                        penalizedDistance += 1.0;

                    if (incoming.SourceNode.DistanceToTarget > (targetNode.DistanceToTarget + penalizedDistance))
                    {
                        if (untouchedNodes.Contains(incoming.SourceNode) && !touchedNodes.Contains(incoming.SourceNode))
                        {
                            untouchedNodes.Remove(incoming.SourceNode);
                            touchedNodes.Add(incoming.SourceNode);
                        }

                        incoming.SourceNode.DistanceToTarget = (targetNode.DistanceToTarget + penalizedDistance);
                        incoming.SourceNode.PathToTarget = targetNode;
                        incoming.SourceNode.NameToTarget = incoming.LinkName;
                        incoming.SourceNode.PathIsRunway = incoming.IsRunway;
                        incoming.SourceNode.BearingToTarget = incoming.Bearing;
                    }
                }

                touchedNodes.Remove(targetNode);
            }
        }




        private void readAirportRecord(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);

            double latitude1 = double.Parse(tokens[9]) * VortexMath.Deg2Rad;
            double longitude1 = double.Parse(tokens[10]) * VortexMath.Deg2Rad;
            double latitude2 = double.Parse(tokens[18]) * VortexMath.Deg2Rad;
            double longitude2 = double.Parse(tokens[19]) * VortexMath.Deg2Rad;

            _runways.Add(new Runway(tokens[8], latitude1, longitude1, double.Parse(tokens[11]) / 1000.0, latitude2, longitude2));
            _runways.Add(new Runway(tokens[17], latitude2, longitude2, double.Parse(tokens[20]) / 1000.0, latitude1, longitude1));
        }

        private void readTaxiNode(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
            ulong id = ulong.Parse(tokens[4]);
            _nodeDict[id] = new TaxiNode(id, tokens[1], tokens[2]);
            _nodeDict[id].Name = string.Join(" ", tokens.Skip(5));
        }

        private void readTaxiEdge(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
            ulong va = ulong.Parse(tokens[1]);
            ulong vb = ulong.Parse(tokens[2]);
            bool isRunway = (tokens[4][0] != 't');
            bool isTwoWay = (tokens[3][0] == 't');
            int maxSize = isRunway ? 5 : (int)(tokens[4][8] - 'A'); // todo: make more robust / future proof
            string linkName = tokens.Length > 5 ? string.Join(" ", tokens.Skip(5)) : "";

            TaxiEdge prev = _edges.SingleOrDefault(e => (e.StartNodeId == va && e.EndNodeId == vb));
            if (prev != null)
                // todo: report warning
                prev.MaxSize = Math.Max(prev.MaxSize, maxSize);
            else
                _edges.Add(new TaxiEdge(va, vb, isRunway, maxSize, linkName));

            if (isTwoWay)
            {
                prev = _edges.SingleOrDefault(e => (e.StartNodeId == vb && e.EndNodeId == va));
                if (prev != null)
                    // todo: report warning
                    prev.MaxSize = Math.Max(prev.MaxSize, maxSize);
                else
                    _edges.Add(new TaxiEdge(vb, va, isRunway, maxSize, linkName));
            }


            _nodeDict[vb].AddEdgeFrom(_nodeDict[va], maxSize, isRunway, linkName);
            if (isTwoWay)
            {
                _nodeDict[va].AddEdgeFrom(_nodeDict[vb], maxSize, isRunway, linkName);
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
