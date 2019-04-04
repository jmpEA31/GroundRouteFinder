using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GroundRouteFinder.LogSupport;
using GroundRouteFinder.Output;

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
        public IEnumerable<TaxiNode> TaxiNodes { get { return _taxiNodes; } }

        private List<Parking> _parkings; /* could be gate, helo, tie down, ... but 'parking' improved readability of some of the code */
        public List<Parking> Parkings { get { return _parkings; } }

        private List<Runway> _runways;
        public List<Runway> Runways { get { return _runways; } }

        private List<TaxiEdge> _edges;

        public List<LineElement> _lines;
        public bool inLine = false;

        private static readonly char[] _splitters = { ' ' };

        LineElement cle = null;

        private TrafficFlow _flows = new TrafficFlow();

        private Dictionary<string, int> _parkingNameDuplicates;


        public Airport()
            : base()
        {
            _parkingNameDuplicates = new Dictionary<string, int>();
            ICAO = "";
        }

        internal bool Analyze(string file, string icao)
        {
            ReadData(file);

            return ((_nodeDict.Count > 0) && (_edges.Count > 0) && (_parkings.Count > 0));
        }

        public void Process()
        {
            Preprocess();
        }


        public void Load(string name)
        {
            ReadData(name);
            Preprocess();
        }

        private void ReadData(string name)
        {
            _parkingNameDuplicates.Clear();
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
                    ReadAirportRecord(lines[i]);
                }
                else if (lines[i].StartsWith("100 "))
                {
                    ReadRunwayRecord(lines[i]);
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
                    ReadTaxiNode(lines[i]);
                }
                else if (lines[i].StartsWith("1202 "))
                {
                    ReadTaxiEdge(lines[i]);
                }
                else if (lines[i].StartsWith("1204 "))
                {
                    ReadTaxiEdgeOperations(lines[i]);
                }
                else if (lines[i].StartsWith("1300 "))
                {
                    ReadStartPoint(lines[i]);
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

        public void Preprocess()
        {
            // Filter out nodes with links (probably nodes for the vehicle network)
            _taxiNodes = _nodeDict.Values.Where(v => v.IncomingEdges.Count > 0);

            // Filter out parkings with operation type none.
            _parkings = _parkings.Where(p => p.Operation != OperationType.None).ToList();

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

            Dictionary<WorldTrafficAircraftType, int> numberOfParkingsPerWTType = new Dictionary<WorldTrafficAircraftType, int>();
            for (WorldTrafficAircraftType cat = WorldTrafficAircraftType.Fighter; cat <= WorldTrafficAircraftType.Max; cat++)
            {
                numberOfParkingsPerWTType[cat] = 0;
            }

            foreach (Parking parking in _parkings)
            {
                parking.DetermineWtTypes();
                parking.DetermineTaxiOutLocation(_taxiNodes); // Move this to first if we need pushback info in the parking def
                numberOfParkingsPerCategory[parking.MaxSize]++;

                foreach (XPlaneAircraftType wtt in parking.XpTypes)
                {
                    WorldTrafficAircraftType t = AircraftTypeConverter.WTTypeFromXPlaneTypeAndCat(parking.MaxSize, wtt);
                    numberOfParkingsPerWTType[t]++;
                }
                //parking.FindNearestLine(_lines);
            }

            Log($"Parkings by Category: {string.Join(" ", numberOfParkingsPerCategory.Select(kvp => kvp.Key.ToString() + ": " + kvp.Value.ToString()))}");
            Log($"Parkings by WorldTraffic type: {string.Join(" ", numberOfParkingsPerWTType.Select(kvp => kvp.Key.ToString() + ": " + kvp.Value.ToString()))}");

            StringBuilder sb = new StringBuilder();
            _flows.Analyze(sb);

            foreach (Runway r in _runways)
            {
                r.Analyze(_taxiNodes, _edges);
            }
        }

        public bool AnalyzeFlows(StringBuilder sb)
        {
            return _flows.Analyze(sb);
        }

        public int WriteParkingDefs()
        {
            // Parking preprocessing
            string parkingDefPath = Path.Combine(Settings.WorldTrafficParkingDefs, ICAO);
            Settings.DeleteDirectoryContents(parkingDefPath);

            foreach (Parking parking in _parkings)
            {
                parking.WriteParkingDef();
            }
            return _parkings.Count;
        }

        public bool WriteOperations()
        {
            string operationFile = Path.Combine(Settings.WorldTrafficOperations, $"{ICAO}.txt");
            File.Delete(operationFile);
            _flows.Write(ICAO);
            return _flows.RuleSetOk;
        }


        public int FindInboundRoutes(bool normalOutput, ProgressBar progress)
        {
            string outputPath = normalOutput ? Path.Combine(Settings.WorldTrafficGroundRoutes, "Arrival") : Settings.ArrivalFolderKML;
            outputPath = Path.Combine(outputPath, ICAO);
            Settings.DeleteDirectoryContents(outputPath);

            int count = 0;

            progress.Minimum = 0;
            progress.Maximum = _parkings.Count * (int)XPlaneAircraftCategory.Max;
            progress.Value = 0;

            foreach (Parking parking in _parkings)
            {
                InboundResults ir = new InboundResults(_edges, parking);

                progress.Value += (XPlaneAircraftCategory.Max - (parking.MaxSize + 1));

                for (XPlaneAircraftCategory size = parking.MaxSize; size >= XPlaneAircraftCategory.A; size--)
                {
                    // Nearest node should become 'closest to computed pushback point'
                    FindShortestPaths(_taxiNodes, parking.NearestNode, size);

                    // Pick the runway exit points for the selected size
                    foreach (Runway r in _runways)
                    {
                        foreach (KeyValuePair<TaxiNode, List<ExitPoint>> exit in r.ExitGroups)
                        {
                            double bestDistance = double.MaxValue;
                            double bestTurnAngle = double.MaxValue;
                            ExitPoint bestExit = null;

                            foreach (ExitPoint ep in exit.Value)
                            {
                                if (ep.OffRunwayNode.NextNodeToTarget != ep.OnRunwayNode)
                                {
                                    if ((ep.OffRunwayNode.DistanceToTarget < bestDistance / 1.2) || (bestTurnAngle - Math.Abs(ep.TurnAngle) > VortexMath.Deg020Rad) ||
                                        (ep.OffRunwayNode.DistanceToTarget < bestDistance && Math.Abs(ep.TurnAngle) < bestTurnAngle))
                                    {
                                        bestExit = ep;
                                        bestDistance = ep.OffRunwayNode.DistanceToTarget;
                                        bestTurnAngle = Math.Abs(ep.TurnAngle);
                                    }
                                }
                            }

                            if (bestExit != null)
                                ir.AddResult(size, bestExit.OnRunwayNode, bestExit.OffRunwayNode, r, bestExit.LandingLengthUsed);
                        }
                    }
                    progress.Value++;
                    progress.Update();
                }

                count += ir.WriteRoutes(outputPath, !normalOutput);
            }

            progress.Maximum++; progress.Value++; progress.Maximum--; progress.Value = progress.Maximum; // Work around for a side effect caused by windows animating the progress bar
            progress.Update();
            return count;
        }
    
        public int FindOutboundRoutes(bool normalOutput, ProgressBar progress)
        {
            string outputPath = normalOutput ? Path.Combine(Settings.WorldTrafficGroundRoutes, "Departure") : Settings.DepartureFolderKML;
            outputPath = Path.Combine(outputPath, ICAO);
            Settings.DeleteDirectoryContents(outputPath);

            int count = 0;

            progress.Minimum = 0;
            progress.Maximum = _runways.Sum(r => r.EntryGroups.Count) * (int)XPlaneAircraftCategory.Max;
            progress.Value = 0;

            // for each runway
            foreach (Runway runway in _runways)
            {
                // for each takeoff spot
                foreach (KeyValuePair<TaxiNode, List<EntryPoint>> entryGroup in runway.EntryGroups)
                {
                    OutboundResults or = new OutboundResults(_edges, runway);
                    // for each size
                    for (XPlaneAircraftCategory size = XPlaneAircraftCategory.F; size >= XPlaneAircraftCategory.A; size--)
                    {
                        foreach (EntryPoint ep in entryGroup.Value)
                        {
                            // find shortest path from each parking to each takeoff spot considering each entrypoint
                            FindShortestPaths(_taxiNodes, ep.OffRunwayNode, size);
                            foreach (Parking parking in _parkings)
                            {
                                or.AddResult(size, parking.NearestNode, parking, entryGroup.Key, ep);
                            }
                        }
                        progress.Value++;
                        progress.Update();
                    }
                    count += or.WriteRoutes(outputPath, !normalOutput);
                }
            }

            progress.Maximum++; progress.Value++; progress.Maximum--; progress.Value = progress.Maximum; // Work around for a side effect caused by windows animating the progress bar
            progress.Update();

            return count;
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
        private static void FindShortestPaths(IEnumerable<TaxiNode> nodes, TaxiNode targetNode, XPlaneAircraftCategory targetCategory)
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

        internal void DebugParkings()
        {
            using (InvariantWriter sw = new InvariantWriter(Path.Combine(Settings.DataFolder, "starts.csv"), Encoding.UTF8))
            {
                sw.WriteLine("latitude,longitude,name\n");
                foreach (Parking parking in _parkings)
                {
                    sw.WriteLine($"{parking.Latitude * VortexMath.Rad2Deg},{parking.Longitude * VortexMath.Rad2Deg},{parking.Name}");
                }
            }

            using (InvariantWriter sw = new InvariantWriter(Path.Combine(Settings.DataFolder, "pushback.csv"), Encoding.UTF8))
            {
                sw.WriteLine("latitude,longitude,name\n");
                foreach (Parking parking in _parkings)
                {
                    sw.WriteLine($"{parking.PushBackLatitude * VortexMath.Rad2Deg},{parking.PushBackLongitude * VortexMath.Rad2Deg},{parking.Name}");
                }
            }
        }

        internal void DebugAtcNodes()
        {
            using (InvariantWriter sw = new InvariantWriter(Path.Combine(Settings.DataFolder, "atcnodes.csv"), Encoding.UTF8))
            {
                sw.WriteLine("latitude,longitude,name\n");

                foreach (TaxiNode node in _taxiNodes)
                {
                    sw.WriteLine($"{node.Latitude * VortexMath.Rad2Deg},{node.Longitude * VortexMath.Rad2Deg},{node.Id}");
                }
            }
        }

        private void ReadAirportRecord(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);

            ICAO = tokens[4];
        }

        private void ReadRunwayRecord(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);

            double latitude1 = VortexMath.ParseDegreesToRadians(tokens[9]);
            double longitude1 = VortexMath.ParseDegreesToRadians(tokens[10]); 
            double latitude2 = VortexMath.ParseDegreesToRadians(tokens[18]); 
            double longitude2 = VortexMath.ParseDegreesToRadians(tokens[19]);

            Runway r1 = new Runway(tokens[8], latitude1, longitude1, VortexMath.Parse(tokens[11]) / 1000.0);
            r1.LogMessage += RelayMessage;

            Runway r2 = new Runway(tokens[17], latitude2, longitude2, VortexMath.Parse(tokens[20]) / 1000.0);
            r2.LogMessage += RelayMessage;

            r1.OppositeEnd = r2;
            r2.OppositeEnd = r1;

            _runways.Add(r1);
            _runways.Add(r2);
        }

        private void ReadLineSegment(string line)
        {
            if (cle == null)
            {
                cle = new LineElement();
                string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
                double latitude1 = VortexMath.ParseDegreesToRadians(tokens[1]);
                double longitude1 = VortexMath.ParseDegreesToRadians(tokens[2]);
                cle.Latitude = latitude1;
                cle.Longitude = longitude1;
                _lines.Add(cle);
            }
            else
            {
                // skipping intermediates now.
            }
        }

        private void ReadLineEnd(string line)
        {
            if (inLine)
            {
                string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
                LineElement le = new LineElement();
                double latitude1 = VortexMath.ParseDegreesToRadians(tokens[1]);
                double longitude1 = VortexMath.ParseDegreesToRadians(tokens[2]);
                le.Latitude = latitude1;
                le.Longitude = longitude1;
                cle.Segments.Add(le);
                cle = null;
                inLine = false;
            }
        }


        private void ReadTaxiNode(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
            uint id = uint.Parse(tokens[4]);
            _nodeDict[id] = new TaxiNode(id, tokens[1], tokens[2])
            {
                Name = string.Join(" ", tokens.Skip(5))
            };
        }

        private void ReadTaxiEdge(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
            uint va = uint.Parse(tokens[1]);
            uint vb = uint.Parse(tokens[2]);
            bool isRunway = (tokens[4][0] != 't');  // taxiway_X or runway
            bool isTwoWay = (tokens[3][0] == 't');  // oneway or twoway

            XPlaneAircraftCategory maxSize;
            if (isRunway || tokens[4].Length < 9)
                maxSize = (XPlaneAircraftCategory.Max - 1);
            else
                maxSize = (XPlaneAircraftCategory)(tokens[4][8] - 'A');

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

        private void ReadTaxiEdgeOperations(string line)
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

        private void ReadStartPoint(string line)
        {
            string[] tokens = line.Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
            string[] xpTypes = tokens[5].Split('|');

            Parking sp = new Parking(this)
            {
                Latitude = VortexMath.ParseDegreesToRadians(tokens[1]),
                Longitude = VortexMath.ParseDegreesToRadians(tokens[2]),
                Bearing = ((VortexMath.Parse(tokens[3]) + 540) * VortexMath.Deg2Rad) % (VortexMath.PI2) - Math.PI,
                LocationType = StartUpLocationTypeConverter.FromString(tokens[4]),
                XpTypes = AircraftTypeConverter.XPlaneTypesFromStrings(xpTypes),
                Name = string.Join(" ", tokens.Skip(6))
            };

            if (Settings.FixDuplicateParkingNames)
            {
                if (_parkings.Count(p => p.Name == sp.Name) > 0)
                {
                    if (!_parkingNameDuplicates.ContainsKey(sp.Name))
                    {
                        _parkingNameDuplicates.Add(sp.Name, 1);
                    }

                    _parkingNameDuplicates[sp.Name]++;
                    sp.Name += $" - {_parkingNameDuplicates[sp.Name]}";
                    Logger.Log($"Renamed a parking to: {sp.Name}");
                }
            }

            _parkings.Add(sp);
        }
    }
}
