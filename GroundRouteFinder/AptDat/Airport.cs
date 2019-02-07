﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GroundRouteFinder.AptDat
{
    public class LineElement : LocationObject
    {
        public List<LineElement> Segments;
        public LineElement()
            : base()
        {
            Segments = new List<LineElement>();
        }
    }

    public class Airport : LogEmitter
    {
        public string ICAO;
        
        private Dictionary<uint, TaxiNode> _nodeDict;
        private IEnumerable<TaxiNode> _taxiNodes;
        private List<Parking> _parkings; /* could be gate, helo, tie down, ... but 'parking' improved readability of some of the code */
        private List<Runway> _runways;
        private List<TaxiEdge> _edges;

        private bool hasFlowRules = false;
        private bool hasVfrRules = false;

        public List<LineElement> _lines;
        public bool inLine = false;

        private static char[] _splitters = { ' ' };

        LineElement cle = null;

        private TrafficFlow _flows = new TrafficFlow();

        public Airport()
            : base()
        {
            ICAO = "";
        }

        internal bool Analyze(string file, string icao)
        {
            load(file);

            return ((_nodeDict.Count > 0) && (_edges.Count > 0) && (_parkings.Count > 0));
        }

        public void Process()
        {
            preprocess();
        }


        public void Load(string name)
        {
            load(name);
            preprocess();
        }

        private void load(string name)
        {
            _nodeDict = new Dictionary<uint, TaxiNode>();
            _parkings = new List<Parking>();
            _runways = new List<Runway>();
            _edges = new List<TaxiEdge>();
            _lines = new List<LineElement>();

            string[] lines = File.ReadAllLines(name);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("1 "))
                {
                    readAirportRecord(lines[i]);
                }
                else if (lines[i].StartsWith("100 "))
                {
                    readRunwayRecord(lines[i]);
                }

                // Possibly use this to improve pushback
                //else if (lines[i].StartsWith("120 "))
                //{
                //    inLine = true;
                //}
                //else if (inLine && (lines[i].StartsWith("111 ") || lines[i].StartsWith("112 ")))
                //{
                //    readLineSegment(lines[i]);
                //}
                //else if (inLine && (lines[i].StartsWith("115 ") || lines[i].StartsWith("116 ")))
                //{
                //    readLineEnd(lines[i]);
                //}
                //else if (lines[i].StartsWith("100 "))
                //{
                //    readRunwayRecord(lines[i]);
                //}

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
                    _parkings.Last().SetMetaData(
                                    (XPlaneAircraftCategory)(tokens[1][0] - 'A'),
                                    tokens[2],
                                    tokens.Skip(3));
                }
                else
                {
                    // Finally see if the line contains traffic flow information
                    _flows.ParseInfo(lines[i], _runways);
                }
            }

            Log($"{ICAO} Parkings: {_parkings.Count} Runways: {_runways.Count} Nodes: {_nodeDict.Count()} TaxiPaths: {_edges.Count}");
        }

        public void preprocess()
        {
            // Filter out nodes with links (probably nodes for the vehicle network)
            _taxiNodes = _nodeDict.Values.Where(v => v.IncomingEdges.Count > 0);

            // Filter out parkings with operation type none.
            _parkings = _parkings.Where(p => p.Operation != OperationType.None).ToList();

            var ps = _parkings.GroupBy(p => p.Name);
            foreach (var psv in ps)
            {
                if (psv.Count() > 1)
                    Log($"WARNING Duplicate parking names in apt source: <{psv.First().Name}> occurs {psv.Count()} times.");
            }

            // With unneeded nodes gone, parse the lat/lon string and convert the values to radians
            foreach (TaxiNode v in _taxiNodes)
            {
                v.ComputeLonLat();
            }

            // Compute distance and bearing of each edge
            foreach (TaxiEdge edge in _edges)
            {
                edge.Compute();
            }

            Dictionary<XPlaneAircraftCategory, int> numberOfParkingsPerCategory = new Dictionary<XPlaneAircraftCategory, int>();
            for (XPlaneAircraftCategory cat = XPlaneAircraftCategory.A; cat < XPlaneAircraftCategory.Max;cat++)
            {
                numberOfParkingsPerCategory[cat] = 0;
            }

            foreach (Parking parking in _parkings)
            {
                parking.DetermineWtTypes();
                parking.DetermineTaxiOutLocation(_taxiNodes); // Move this to first if we need pushback info in the parking def
                numberOfParkingsPerCategory[parking.MaxSize]++;
                //parking.FindNearestLine(_lines);
            }

            Log($"Parkings: {string.Join(" ", numberOfParkingsPerCategory.Select(kvp => kvp.Key.ToString() + ": " + kvp.Value.ToString()))}");

            _flows.Analyze();

            // Find taxi links that are the actual runways
            //  Then find applicable nodes for entering the runway and find the nodes off the runway connected to those
            foreach (Runway r in _runways)
            {
                r.Analyze(_taxiNodes, _edges);
                if (r.RunwayExits.Count > 0)
                    Log($"Rwy {r.Designator,3} Exits: {r.RunwayExits.Count()} ({string.Join(", ", r.RunwayExits.Values.OrderBy(re=>re.ExitDistance).Select(re=>(re.ExitDistance/VortexMath.Foot2Km).ToString("0")))} ft)");
                if (r.TakeOffSpots.Count > 0)
                {
                    Log($"Rwy {r.Designator,3} Entry: {r.TakeOffSpots.Count()} ({string.Join(", ", r.TakeOffSpots.Select(to => (to.TakeOffLengthRemaining / VortexMath.Foot2Km).ToString("0")))} ft)");
                    foreach (var tos in r.TakeOffSpots)
                    {
                        Log($" {tos.TakeOffNode} {tos.TakeOffLengthRemaining / VortexMath.Foot2Km:0} {string.Join("-", tos.EntryPoints.Select(ep => ep.Id))}");
                    }
                }
            }
        }

        public void WriteParkingDefs()
        {
            // Parking preprocessing
            string parkingDefPath = Path.Combine(Settings.WorldTrafficParkingDefs, ICAO);
            Settings.DeleteDirectoryContents(parkingDefPath);

            foreach (Parking parking in _parkings)
            {
                parking.WriteDef();
            }
        }

        public void WriteOperations()
        {
            string operationFile = Path.Combine(Settings.WorldTrafficOperations, $"{ICAO}.txt");
            File.Delete(operationFile);
            _flows.Write(ICAO);
        }


        public void FindInboundRoutes(bool normalOutput)
        {
            string outputPath = normalOutput ? Path.Combine(Settings.WorldTrafficGroundRoutes, "Arrival") : Settings.ArrivalFolderKML;
            outputPath = Path.Combine(outputPath, ICAO);
            Settings.DeleteDirectoryContents(outputPath);

            foreach (Parking parking in _parkings)
            {
                InboundResults ir = new InboundResults(_edges, parking);
                for (XPlaneAircraftCategory size = parking.MaxSize; size >= XPlaneAircraftCategory.A; size--)
                {
                    // Nearest node should become 'closest to computed pushback point'
                    findShortestPaths(_taxiNodes, parking.NearestNode, size);

                    // Pick the runway exit points for the selected size
                    foreach (Runway r in _runways)
                    {
                        foreach (Runway.RunwayExitNode exit in r.RunwayExits.Values)
                        {
                            double bestDistance = double.MaxValue;
                            double bestTurnAngle = double.MaxValue;
                            Runway.RunwayExit bestExit = null;

                            if (exit.LeftExit != null)
                            {
                                bestExit = exit.LeftExit;
                                bestDistance = exit.LeftExit.OffRunwayNode.DistanceToTarget;
                                bestTurnAngle = exit.LeftExit.TurnAngle;
                            }

                            if (exit.RightExit != null)
                            {
                                if ((exit.RightExit.OffRunwayNode.DistanceToTarget < (bestDistance / 1.2) || (bestTurnAngle - exit.RightExit.TurnAngle) > VortexMath.Deg020Rad) ||         // Route 20% shorter, or turn 20 degrees less sharp
                                    (exit.RightExit.OffRunwayNode.DistanceToTarget < bestDistance && exit.RightExit.TurnAngle < bestTurnAngle))                                            // Or just shorter and less sharp
                                {
                                    bestExit = exit.RightExit;
                                }
                            }

                            if (bestExit.OffRunwayNode.NextNodeToTarget != bestExit.OnRunwayNode)
                                ir.AddResult(size, bestExit.OnRunwayNode, bestExit.OffRunwayNode, r, bestExit.ExitDistance);
                        }
                    }
                }

                if (normalOutput)
                    ir.WriteRoutes(outputPath);
                else
                    ir.WriteRoutesKML(outputPath);
            }
        }
    

/* 
 * Multi threading experiment:
 * 
        private class FindThreadParams
        {
            public string outputPath;
            public List<Parking> thrParkings;
            public List<TaxiEdge> thrEdges;
            public List<TaxiNode> thrNodes;
            public List<Runway> thrRunways;
        }

        private static void FindInboundRoutesThread(object para)
        {
            FindThreadParams inParam = para as FindThreadParams;

            foreach (Parking parking in inParam.thrParkings)
            {
                InboundResults ir = new InboundResults(inParam.thrEdges, parking);
                for (XPlaneAircraftCategory size = parking.MaxSize; size >= XPlaneAircraftCategory.A; size--)
                {
                    // Nearest node should become 'closest to computed pushback point'
                    findShortestPaths(inParam.thrNodes, parking.NearestNode, size);

                    // Pick the runway exit points for the selected size
                    foreach (Runway r in inParam.thrRunways)
                    {
                        foreach (Runway.RunwayNodeUsage use in Settings.SizeToUsage[size])
                        {
                            Runway.UsageNodes exitNodes = r.GetNodesForUsage(use);
                            if (exitNodes == null)
                                continue;

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
                                ir.AddResult(size, usage.OnRunwayNode, usage.OffRunwayNode, r);
                            }
                        }
                    }
                }

                //if (normalOutput)
                ir.WriteRoutes(inParam.outputPath);
                //else
                //    ir.WriteRoutesKML(outputPath);
            }
        }
*/

        public void FindOutboundRoutes(bool normalOutput)
        {
            string outputPath = normalOutput ? Path.Combine(Settings.WorldTrafficGroundRoutes, "Departure") : Settings.DepartureFolderKML;
            outputPath = Path.Combine(outputPath, ICAO);
            Settings.DeleteDirectoryContents(outputPath);

            // for each runway
            foreach (Runway runway in _runways)
            {
                // for each takeoff spot
                foreach (RunwayTakeOffSpot takeoffSpot in runway.TakeOffSpots)
                {
                    OutboundResults or = new OutboundResults(_edges, runway);
                    // for each size
                    for (XPlaneAircraftCategory size = XPlaneAircraftCategory.F; size >= XPlaneAircraftCategory.A; size--)
                    {
                        // find shortest path from each parking to each takeoff spot considering each entrypoint
                        foreach (TaxiNode runwayEntryNode in takeoffSpot.EntryPoints)
                        {
                            double remainingRunway = VortexMath.DistanceKM(runwayEntryNode.Latitude, runwayEntryNode.Longitude, runway.OppositeLatitude, runway.OppositeLongitude);
                            findShortestPaths(_taxiNodes, runwayEntryNode, size);
                            foreach (Parking parking in _parkings)
                            {
                                or.AddResult(size, parking.NearestNode, parking, takeoffSpot, remainingRunway);
                            }
                        }
                    }
                    if (normalOutput)
                        or.WriteRoutes(outputPath);
                    else
                        or.WriteRoutesKML(outputPath);
                }
            }
        }

        /// <summary>
        /// IComparer for sorting the nodes by distance
        /// </summary>
        private class ShortestPathComparer : IComparer<TaxiNode>
        {
            public int Compare(TaxiNode a, TaxiNode b)
            {
                return a.DistanceToTarget.CompareTo(b.DistanceToTarget);
            }
        }

        /// <summary>
        /// Dijkstra... goes through the full network finding shortest path from every node to the target so
        /// that afterwards we can cherrypick the starting nodes we are actually interested in.
        /// </summary>
        /// <param name="nodes">The node network</param>
        /// <param name="targetNode">Here do we go now</param>
        /// <param name="targetCategory">Minimum Cat (A-F) that needs to be supported by the route</param>
        private static void findShortestPaths(IEnumerable<TaxiNode> nodes, TaxiNode targetNode, XPlaneAircraftCategory targetCategory)
        {
            List<TaxiNode> untouchedNodes = nodes.ToList();
            List<TaxiNode> touchedNodes = new List<TaxiNode>();

            // Reset previously found paths
            foreach (TaxiNode node in nodes)
            {
                node.DistanceToTarget = double.MaxValue;
                node.NextNodeToTarget = null;
            }

            // Setup the targetnode
            targetNode.DistanceToTarget = 0;
            targetNode.NextNodeToTarget = null;

            // Assign distances to all incoming edges of the target
            foreach (TaxiEdge incoming in targetNode.IncomingEdges)
            {
                // Skip taxiways that are too small
                if (targetCategory > incoming.MaxCategory)
                    continue;

                // Mark the other side of the incoming edge as touched...
                if (untouchedNodes.Contains(incoming.StartNode) && !touchedNodes.Contains(incoming.StartNode))
                {
                    untouchedNodes.Remove(incoming.StartNode);
                    touchedNodes.Add(incoming.StartNode);
                }

                // And set the properties of the path
                incoming.StartNode.DistanceToTarget = incoming.DistanceKM;
                incoming.StartNode.NextNodeToTarget = targetNode;
                incoming.StartNode.NameToTarget = incoming.LinkName;
                incoming.StartNode.PathIsRunway = incoming.IsRunway;
                incoming.StartNode.BearingToTarget = VortexMath.BearingRadians(incoming.StartNode, targetNode);
            }

            // Remove the target node completely, it's done.
            untouchedNodes.Remove(targetNode);

            // instantiate the comparer for taxinode list sorting
            ShortestPathComparer spc = new ShortestPathComparer();

            // Now rinse and repeat will we still have 'touched nodes' (unprocessed nodes with a path to the target)
            while (touchedNodes.Count() > 0)
            {
                // Pick the next( touched but not finished) node to process, that is: the one which currently has the shortest path to the target
                touchedNodes.Sort(spc);
                TaxiNode currentNode = touchedNodes.First();

                // And set the distances for the nodes with link towards the current node
                foreach (TaxiEdge incoming in currentNode.IncomingEdges)
                {
                    // Skip taxiways that are too small
                    if (targetCategory > incoming.MaxCategory)
                        continue;

                    // Avoid runways unless they are the only option.
                    double distanceToCurrent = incoming.DistanceKM; 
                    if (incoming.IsRunway)
                        distanceToCurrent += 2.0;

                    // If the incoming link + the distance from the current node to the target is smaller
                    // than the so far shortest distance from the node on the otherside of the incoming link to
                    // the target... reroute the path from the otherside through the current node.
                    if ((distanceToCurrent + currentNode.DistanceToTarget) < incoming.StartNode.DistanceToTarget)
                    {
                        if (untouchedNodes.Contains(incoming.StartNode) && !touchedNodes.Contains(incoming.StartNode))
                        {
                            // The 'otherside' node is now ready to be processed
                            untouchedNodes.Remove(incoming.StartNode);
                            touchedNodes.Add(incoming.StartNode);
                        }

                        // Update the path properties
                        incoming.StartNode.DistanceToTarget = (currentNode.DistanceToTarget + distanceToCurrent);
                        incoming.StartNode.NextNodeToTarget = currentNode;
                        incoming.StartNode.NameToTarget = incoming.LinkName;
                        incoming.StartNode.PathIsRunway = incoming.IsRunway;
                        incoming.StartNode.BearingToTarget = incoming.Bearing;
                    }
                }

                // And the current is done. 
                touchedNodes.Remove(currentNode);
            }
        }

        private void readAirportRecord(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);

            ICAO = tokens[4];
        }

        private void readRunwayRecord(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);

            double latitude1 = double.Parse(tokens[9]) * VortexMath.Deg2Rad;
            double longitude1 = double.Parse(tokens[10]) * VortexMath.Deg2Rad;
            double latitude2 = double.Parse(tokens[18]) * VortexMath.Deg2Rad;
            double longitude2 = double.Parse(tokens[19]) * VortexMath.Deg2Rad;

            _runways.Add(new Runway(tokens[8], latitude1, longitude1, double.Parse(tokens[11]) / 1000.0, latitude2, longitude2));
            _runways.Last().LogMessage += RelayMessage;
            _runways.Add(new Runway(tokens[17], latitude2, longitude2, double.Parse(tokens[20]) / 1000.0, latitude1, longitude1));
            _runways.Last().LogMessage += RelayMessage;
        }

        private void readLineSegment(string line)
        {
            if (cle == null)
            {
                cle = new LineElement();
                string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
                double latitude1 = double.Parse(tokens[1]) * VortexMath.Deg2Rad;
                double longitude1 = double.Parse(tokens[2]) * VortexMath.Deg2Rad;
                cle.Latitude = latitude1;
                cle.Longitude = longitude1;
                _lines.Add(cle);
            }
            else
            {
                // skipping intermediates now.
            }
        }

        private void readLineEnd(string line)
        {
            if (inLine)
            {
                string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
                LineElement le = new LineElement();
                double latitude1 = double.Parse(tokens[1]) * VortexMath.Deg2Rad;
                double longitude1 = double.Parse(tokens[2]) * VortexMath.Deg2Rad;
                le.Latitude = latitude1;
                le.Longitude = longitude1;
                cle.Segments.Add(le);
                cle = null;
                inLine = false;
            }
        }


        private void readTaxiNode(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
            uint id = uint.Parse(tokens[4]);
            _nodeDict[id] = new TaxiNode(id, tokens[1], tokens[2]);
            _nodeDict[id].Name = string.Join(" ", tokens.Skip(5));
        }

        private void readTaxiEdge(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
            uint va = uint.Parse(tokens[1]);
            uint vb = uint.Parse(tokens[2]);
            bool isRunway = (tokens[4][0] != 't');  // taxiway_X or runway
            bool isTwoWay = (tokens[3][0] == 't');  // oneway or twoway

            XPlaneAircraftCategory maxSize = isRunway ? (XPlaneAircraftCategory.Max-1) : (XPlaneAircraftCategory)(tokens[4][8] - 'A');
            string linkName = tokens.Length > 5 ? string.Join(" ", tokens.Skip(5)) : "";

            TaxiNode startNode = _nodeDict[va];
            TaxiNode endNode = _nodeDict[vb];

            TaxiEdge outgoingEdge = _edges.SingleOrDefault(e => (e.StartNode.Id == va && e.EndNode.Id == vb));
            if (outgoingEdge != null)
                // todo: report warning
                outgoingEdge.MaxCategory = (XPlaneAircraftCategory)Math.Max((int)outgoingEdge.MaxCategory, (int)maxSize);
            else
            {
                outgoingEdge = new TaxiEdge(startNode, endNode, isRunway, maxSize, linkName);
                _edges.Add(outgoingEdge);
            }

            TaxiEdge incomingEdge = null;
            if (isTwoWay)
            {
                incomingEdge = _edges.SingleOrDefault(e => (e.StartNode.Id == vb && e.EndNode.Id == va));
                if (incomingEdge != null)
                    // todo: report warning
                    incomingEdge.MaxCategory = (XPlaneAircraftCategory)Math.Max((int)incomingEdge.MaxCategory, (int)maxSize);
                else
                {
                    incomingEdge = new TaxiEdge(endNode, startNode, isRunway, maxSize, linkName);
                    _edges.Add(incomingEdge);

                    incomingEdge.ReverseEdge = outgoingEdge;
                    outgoingEdge.ReverseEdge = incomingEdge;
                }
            }

            endNode.AddEdgeFrom(outgoingEdge);
            if (isTwoWay)
            {
                startNode.AddEdgeFrom(incomingEdge);
            }
        }

        private void readTaxiEdgeOperations(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
            string[] rwys = tokens[2].Split(',');
            TaxiEdge lastEdge = _edges.Last();
            lastEdge.ActiveZone = true;
            lastEdge.ActiveForRunways.AddRange(rwys);
            lastEdge.ActiveForRunways = lastEdge.ActiveForRunways.Distinct().ToList();
            if (lastEdge.ReverseEdge != null)
            {
                lastEdge.ReverseEdge.ActiveZone = true;
                lastEdge.ReverseEdge.ActiveForRunways.AddRange(rwys);
                lastEdge.ReverseEdge.ActiveForRunways = lastEdge.ReverseEdge.ActiveForRunways.Distinct().ToList();
            }
        }

        private void readStartPoint(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
            string[] xpTypes = tokens[5].Split('|');

            Parking sp = new Parking(this);
            sp.Latitude = double.Parse(tokens[1]) * VortexMath.Deg2Rad;
            sp.Longitude = double.Parse(tokens[2]) * VortexMath.Deg2Rad;
            sp.Bearing = ((double.Parse(tokens[3]) + 540) * VortexMath.Deg2Rad) % (VortexMath.PI2) - Math.PI;
            sp.Type = tokens[4];
            sp.XpTypes = AircraftTypeConverter.XPlaneTypesFromStrings(xpTypes);
            sp.Name = string.Join(" ", tokens.Skip(6));
            _parkings.Add(sp);
        }
    }
}
