using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace GroundRouteFinder
{
    public partial class Form1 : Form
    {
        private Dictionary<ulong, TaxiNode> _vertices;
        private List<Parking> _startPoints;
        private List<Runway> _runways;
        private DateTime _start;
        private static char[] _splitters = { ' ' };
        private Dictionary<string, RunwayEdges> _runwayEdges;

        protected class Edge
        {
            public bool ActiveZone;
            public string ActiveFor;
            public ulong Node1;
            public ulong Node2;
        }

        private List<Edge> _edges;

        public Form1()
        {
            InitializeComponent();
        }

        private void logElapsed(string message = "")
        {
            rtb.AppendText($"{(DateTime.Now - _start).TotalSeconds:00.000} {message}\n");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _start = DateTime.Now;
            rtb.Clear();

            //loadFile("..\\..\\..\\..\\EIDW_Scenery_Pack\\EIDW_Scenery_Pack\\Earth nav data\\apt.dat");
            loadFile("..\\..\\..\\..\\LFPG_Scenery_Pack\\LFPG_Scenery_Pack\\Earth nav data\\apt.dat");
            logElapsed("loading done");
            preprocess();
            logElapsed("preprocessing done");

            logElapsed("Arriving.....");
            FindOutboundRoutes2(_vertices.Values, _startPoints);

            //process(_runways.ToList<TargetNode>(), _startPoints.ToList<TargetNode>());
            logElapsed("Departing.....");
            //List<TaxiNode> _runwayEntries = _runways.SelectMany(r => r.RunwayEntries.SelectMany(re=>re.EntryPoints)).Distinct().ToList<TaxiNode>();
//            process(_startPoints.ToList<TargetNode>(), _runways.ToList<TargetNode>(), true);
            //process(_startPoints.ToList<TargetNode>(), _runwayEntries, true);
            //logElapsed("processing done");
        }

        //        private Dictionary<ulong, Dictionary<int, List<TaxiNode>>> _cache;

        private void FindOutboundRoutes2(IEnumerable<TaxiNode> nodes, IEnumerable<Parking> parkings)
        {
            // for each runway
            foreach (Runway runway in _runways)
            {
                _resultCache = new Dictionary<Parking, Dictionary<TaxiNode, ResultRoute>>();

                // for each takeoff spot
                foreach (RunwayTakeOffSpot takeoffSpot in runway.TakeOffSpots)
                {
                    // for each size
                    for (int size = TaxiNode.Sizes-1; size >= 0; size--)
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

        private Dictionary<Parking, Dictionary<TaxiNode, ResultRoute>> _resultCache;

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
                Edge edge = _edges.Single(e => e.Node1 == node1 && e.Node2 == node2 || e.Node1 == node2 && e.Node2 == node1);
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

        private void writeOutboundRoutes(Runway runway, RunwayTakeOffSpot takeoffSpot, int entry, IEnumerable<TaxiNode> nodes, Parking startPoint, ResultRoute results)
        {
            ResultRoute route = results; //.Value;
            while (route != null)
            {
                if (route.Distance == double.MaxValue)
                    continue;

                Parking from = startPoint; // result.Key;
                LinkedNode link = route.RouteStart;
                TaxiNode nodeToWrite = route.NearestNode;

                List<int> wtSizes = new List<int>();
                foreach (int size in route.ValidForSizes)
                {
                    switch (size)
                    {
                        case 0: // XPlane type A 'wingspan < 15'
                            wtSizes.AddRange(new int[] { 0, 7, 8, 9 }); // Fighter, Light Jet, Light Prop, Helicopter
                            break;
                        case 1: // XPlane type B 'wingspan < 24'
                            wtSizes.AddRange(new int[] { 5, 6 }); // Medium Jet, Medium Prop
                            break;
                        case 2: // XPlane type C 'wingspan < 36'
                            wtSizes.Add(3); // Large Jet
                            break;
                        case 3: // XPlane type D 'wingspan < 52'
                            wtSizes.Add(4); // Large Prop
                            break;
                        case 4: // XPlane type E 'wingspan < 65'
                            wtSizes.Add(2); // Heavy Jet
                            break;
                        case 5: // XPlane type F 'wingspan < 80'
                        default:
                            wtSizes.Add(1); // Supah Heavy Jet
                            break;
                    }
                }
                int speed = 15;

                string allSizes = string.Join(" ", wtSizes.OrderBy(w => w));



                string sizeName = wtSizes.Count == 10 ? "all" : allSizes.Replace(" ", "");
                string fileName = $"E:\\GroundRoutes\\Departures\\LFPG\\{from.FileNameSafeName}_to_{runway.Number}-{entry}_{sizeName}.txt";
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
                    sw.Write($"{from.Latitude * VortexMath.Rad2Deg} {from.Longitude * VortexMath.Rad2Deg} -2 {from.Bearing * VortexMath.Rad2Deg:0} 0 0 {from.Name}\n");

                    // Write Pushback node, allowing room for turn
                    double addLat = 0;
                    double addLon = 0;

                    // See if we need to skip the first route node
                    if (from.AlternateAfterPushBack != null && from.AlternateAfterPushBack == route.RouteStart.Node)
                    {
                        // Our pushback point is better than the first point of the route
                        nodeToWrite = from.AlternateAfterPushBack;
                    }
                    else if (VortexMath.DistancePyth(from.PushBackLatitude, from.PushBackLongitude, route.NearestNode.Latitude, route.NearestNode.Longitude) < 0.000000001)
                    {
                        // pushback node is the first route point
                        nodeToWrite = route.NearestNode;
                    }

                    // insert one more point here where the plane is pushed a little bit away from the next point
                    if (nodeToWrite != null)
                    {
                        double nextPushBearing = VortexMath.BearingRadians(nodeToWrite.Latitude, nodeToWrite.Longitude, from.PushBackLatitude, from.PushBackLongitude);
                        double turn = VortexMath.TurnAngle(from.Bearing, nextPushBearing);
                        double distance = 0.040 * ((VortexMath.PI - turn) / VortexMath.PI);
                        VortexMath.PointFrom(from.PushBackLatitude, from.PushBackLongitude, from.Bearing, distance, ref addLat, ref addLon);
                        sw.Write($"{addLat * VortexMath.Rad2Deg:0.00000000} {addLon * VortexMath.Rad2Deg:0.00000000} -1 -1 0 0 {link.LinkName}\n");
                        VortexMath.PointFrom(from.PushBackLatitude, from.PushBackLongitude, nextPushBearing, 0.030, ref addLat, ref addLon);
                        sw.Write($"{addLat * VortexMath.Rad2Deg:0.00000000} {addLon * VortexMath.Rad2Deg:0.00000000} 10 -1 0 0 {link.LinkName}\n");
                    }

                    bool wasOnRunway = false;

                    double lastBearing = VortexMath.BearingRadians(from.PushBackLatitude, from.PushBackLongitude, nodeToWrite.Latitude, nodeToWrite.Longitude);
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
                            turnAngle = VortexMath.TurnAngle(lastBearing, nextBearing);
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

        private void loadFile(string name)
        {
            _vertices = new Dictionary<ulong, TaxiNode>();
            _startPoints = new List<Parking>();
            _runways = new List<Runway>();
            _runwayEdges = new Dictionary<string, RunwayEdges>();
            _edges = new List<Edge>();


            string[] lines = File.ReadAllLines(name);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("100 "))
                {
                    string[] tokens = lines[i].Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
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
                else if (lines[i].StartsWith("1201 "))
                {
                    string[] tokens = lines[i].Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
                    ulong id = ulong.Parse(tokens[4]);
                    _vertices[id] = new TaxiNode(id, tokens[1], tokens[2]);
                    _vertices[id].Name = string.Join(" ", tokens.Skip(5));
                }
                else if (lines[i].StartsWith("1202 "))
                {
                    string[] tokens = lines[i].Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
                    ulong va = ulong.Parse(tokens[1]);
                    ulong vb = ulong.Parse(tokens[2]);
                    _edges.Add(new Edge() { Node1 = va, Node2 = vb, ActiveZone = false });

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

                        _runwayEdges[tokens[5]].AddEdge(_vertices[va], _vertices[vb]);
                    }

                    string linkName = tokens.Length > 5 ? string.Join(" ", tokens.Skip(5)) : "";
                    _vertices[vb].AddEdgeFrom(_vertices[va], maxSize, isRunway, linkName);
                    if (tokens[3][0] == 't')
                    {
                        _vertices[va].AddEdgeFrom(_vertices[vb], maxSize, isRunway, linkName);
                    }
                }
                else if (lines[i].StartsWith("1204 "))
                {
                    string[] tokens = lines[i].Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
                    _edges.Last().ActiveZone = true;
                    string[] rwys = tokens[2].Split(',');
                    _edges.Last().ActiveFor = rwys[0];
                }
                else if (lines[i].StartsWith("1300 "))
                {
                    string[] tokens = lines[i].Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens[5] != "helos") // ignore helos for now
                    {
                        Parking sp = new Parking();
                        sp.Latitude = double.Parse(tokens[1]) * VortexMath.Deg2Rad;
                        sp.Longitude = double.Parse(tokens[2]) * VortexMath.Deg2Rad;
                        sp.Bearing = ((double.Parse(tokens[3]) + 540) * VortexMath.Deg2Rad) % (VortexMath.PI2) - Math.PI;
                        sp.Type = tokens[4];
                        sp.Jets = tokens[5];
                        sp.Name = string.Join(" ", tokens.Skip(6));
                        _startPoints.Add(sp);
                    }
                }
                else if (lines[i].StartsWith("1301 "))
                {
                    string[] tokens = lines[i].Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
                    _startPoints.Last().SetLimits((int)(tokens[1][0] - 'A'), tokens[2]);
                }
            }
        }

        private void preprocess()
        {
            // Filter out nodes with links (probably nodes for the vehicle network)
            _vertices = _vertices.Values.Where(v => v.IncomingNodes.Count > 0).ToDictionary(v => v.Id);

            StreamWriter sw = File.CreateText("E:\\GroundRoutes\\taxinodes.csv");
            sw.WriteLine("lat,lon,id,name");

            // With unneeded nodes gone, parse the lat/lon string and convert the values to radians
            foreach (TaxiNode v in _vertices.Values)
            {
                sw.WriteLine($"{v.LatitudeString},{v.LongitudeString},{v.Id},{v.Name}");
                v.ComputeLonLat();
            }
            sw.Dispose();

            // Compute the lengths of each link (in arbitrary units
            // to avoid sin/cos and a find multiplications)
            foreach (TaxiNode v in _vertices.Values)
            {
                v.ComputeDistances();
            }

            // Damn you X plane for not requiring parking spots/gate to be linked
            // to the taxi route network. Why???????
            foreach (Parking sp in _startPoints)
            {
                sp.DetermineTaxiOutLocation(_vertices.Values);
            }

            // Find taxi nodes closest to runways
            foreach (Runway r in _runways)
            {
                double shortestDistance = double.MaxValue;
                double shortestDisplacedDistance = double.MaxValue;

                foreach (TaxiNode vx in _vertices.Values.Where(v => v.IsRunwayEdge))
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
            }

            // Find taxi links that are the actual runways
            foreach (Runway r in _runways)
            {
                var edgesKeys = _runwayEdges.Where(rwe => rwe.Value.HasVertex(r.NearestVertex.Id)).Select(rwe => rwe.Key);
                rtb.AppendText($"Runway: {r.Number}\n Bearing: {r.Bearing * VortexMath.Rad2Deg:0.0}\n Nearest Node: {r.NearestVertex.Id}\n Edges: {string.Join(", ", edgesKeys)}\n");

                string edgeKey = edgesKeys.FirstOrDefault();
                if (!string.IsNullOrEmpty(edgeKey))
                {
                    RunwayEdges rwe = _runwayEdges[edgeKey];
                    string chain;
                    List <TaxiNode> nodes = rwe.FindChainFrom(r.NearestVertex.Id, out chain);
                    rtb.AppendText($" {chain}\n");

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
                        rtb.AppendText($"  {node.Id} Incommming: {node.IncomingNodes.Count} {string.Join(", ", node.IncomingNodes.Select(iv => $"{iv.SourceNode.Id} {iv.Bearing * VortexMath.Rad2Deg:0.0} {iv.IsRunway}"))}\n");
                        foreach (MeasuredNode mn in node.IncomingNodes)
                        {
                            if (mn.IsRunway)
                                continue;

                            double entryAngle = VortexMath.TurnAngle(mn.Bearing, r.Bearing);
                            if (entryAngle <= 0.6 * VortexMath.PI) // allow a turn of roughly 100 degrees, todo: maybe lower this?
                            {
                                rtb.AppendText($"    -> Selected {mn.SourceNode.Id} with entry angle: {entryAngle * VortexMath.Rad2Deg:0.0}\n");
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

                }
            }

            foreach (var kvp in _runwayEdges)
            {
                kvp.Value.Process();
                rtb.AppendText($"Runway: {kvp.Key}, Edges: {kvp.Value.Edges.Count}\n");
            }

        }
    }
}
