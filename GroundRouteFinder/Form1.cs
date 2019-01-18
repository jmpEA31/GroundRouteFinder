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
        private List<StartPoint> _startPoints;
        private List<Runway> _runways;
        private DateTime _start;
        private static char[] _splitters = { ' ' };
        private Dictionary<string, RunwayEdges> _runwayEdges;

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

            //logElapsed("Arriving.....");
            //process(_runways.ToList<TargetNode>(), _startPoints.ToList<TargetNode>());
            logElapsed("Departing.....");
            process(_startPoints.ToList<TargetNode>(), _runways.ToList<TargetNode>(), true);
            logElapsed("processing done");
        }

//        private Dictionary<ulong, Dictionary<int, List<TaxiNode>>> _cache;

        private void process(List<TargetNode> fromNodes, List<TargetNode> toNodes, bool outbound)
        {
            //int cacheHits = 0;
            //_cache = new Dictionary<ulong, Dictionary<int, List<TaxiNode>>>();

            List<TaxiNode> doneNodes = _vertices.Values.ToList();
            List<TaxiNode> touchedNodes = new List<TaxiNode>();
            List<TaxiNode> untouchedNodes = new List<TaxiNode>();

            foreach (TargetNode target in toNodes)
            {
                //if (_cache.ContainsKey(target.NearestVertex.Id))
                //{
                //    cacheHits++;
                //    continue;
                //}

                StringBuilder sb = new StringBuilder();
                for (int size = 0; size < TaxiNode.Sizes; size++)
                {
                    // Could check to see if this really helps performance :D
                    doneNodes.AddRange(touchedNodes);
                    doneNodes.AddRange(untouchedNodes);
                    untouchedNodes = doneNodes;

                    touchedNodes = new List<TaxiNode>();
                    doneNodes = new List<TaxiNode>();

                    TaxiNode v = target.NearestVertex;
                    v.DistanceToTarget = 0;

                    foreach (MeasuredNode vto in v.IncomingVertices)
                    {
                        if (size > vto.MaxSize)
                            continue;

                        if (untouchedNodes.Contains(vto.SourceNode) && !touchedNodes.Contains(vto.SourceNode))
                        {
                            untouchedNodes.Remove(vto.SourceNode);
                            touchedNodes.Add(vto.SourceNode);
                        }

                        vto.SourceNode.DistanceToTarget = vto.RelativeDistance;
                        vto.SourceNode.PathToTarget = v;
                        vto.SourceNode.NameToTarget = vto.LinkName;
                        vto.SourceNode.PathIsRunway = vto.IsRunway;
                    }

                    bool firstNode = true;

                    doneNodes.Add(v);
                    untouchedNodes.Remove(v);

                    while (touchedNodes.Count() > 0)
                    {
                        double min = touchedNodes.Min(a => a.DistanceToTarget);
                        v = touchedNodes.FirstOrDefault(a => a.DistanceToTarget == min);
                        if (v == null)
                            break;

                        foreach (MeasuredNode vto in v.IncomingVertices)
                        {
                            if (size > vto.MaxSize)
                                continue;

                            // Try to force smaller aircraft to take their specific routes
                            double penalizedDistance = vto.RelativeDistance * (1.0 + 2.0 * (vto.MaxSize - size));

                            //if (outbound && firstNode)
                            //{
                            //    Runway rw = target as Runway;
                            //    if (rw.Number == "09R")
                            //    {
                            //        int k = 5;
                            //    }
                            //    double rwBearing = VortexMath.BearingRadians(rw.Latitude, rw.Longitude, rw.OppositeEnd.Latitude, rw.OppositeEnd.Longitude);
                            //    if (Math.Abs(((VortexMath.PI3 + vto.Bearing - rwBearing) % VortexMath.PI2) - VortexMath.PI) < 0.125 * Math.PI)
                            //    {
                            //        // Let's not use a node on the runway heading towards the end of the runway
                            //        penalizedDistance += 2.0;
                            //    }
                            //}

                            if (vto.IsRunway)
                                penalizedDistance += 1.0;

                            if (vto.SourceNode.DistanceToTarget > (v.DistanceToTarget + penalizedDistance))
                            {
                                if (untouchedNodes.Contains(vto.SourceNode) && !touchedNodes.Contains(vto.SourceNode))
                                {
                                    untouchedNodes.Remove(vto.SourceNode);
                                    touchedNodes.Add(vto.SourceNode);
                                }

                                vto.SourceNode.DistanceToTarget = (v.DistanceToTarget + penalizedDistance);
                                vto.SourceNode.PathToTarget = v;
                                vto.SourceNode.NameToTarget = vto.LinkName;
                                vto.SourceNode.PathIsRunway = vto.IsRunway;
                            }
                        }

                        touchedNodes.Remove(v);
                        doneNodes.Add(v);
                        firstNode = false;
                    }

                    foreach (TargetNode fromNode in fromNodes)
                    {
                        StartPoint sp = fromNode as StartPoint;

                        if (fromNode.NearestVertex.PathToTarget != null)
                        {
                            if (outbound)
                            {
                                if (sp.MaxSize < size)
                                    continue;
                            }

                            string wtSizes;
                            switch (size)
                            {
                                case 0: // XPlane type A 'wingspan < 15'
                                    wtSizes = "0 7 8 9"; // Fighter, Light Jet, Light Prop, Helicopter
                                    break;
                                case 1: // XPlane type B 'wingspan < 24'
                                    wtSizes = "5 6"; // Medium Jet, Medium Prop
                                    break;
                                case 2: // XPlane type C 'wingspan < 36'
                                    wtSizes = "3"; // Large Jet
                                    break;
                                case 3: // XPlane type D 'wingspan < 52'
                                    wtSizes = "4"; // Large Prop
                                    break;
                                case 4: // XPlane type E 'wingspan < 65'
                                    wtSizes = "2"; // Heavy Jet
                                    break;
                                case 5: // XPlane type F 'wingspan < 80'
                                default:
                                    wtSizes = "1"; // Supah Heavy Jet
                                    break;
                            }

                            int speed = 12 + size;

                            string fileName = $"E:\\GroundRoutes\\Departures\\LFPG\\{fromNode.FileNameSafeName}_to_{target.FileNameSafeName}_{wtSizes.Replace(" ", "")}.txt";
                            File.Delete(fileName);
                            using (StreamWriter sw = File.CreateText(fileName))
                            {
                                if (outbound)
                                {
                                    sw.Write($"STARTAIRCRAFTTYPE\n{wtSizes}\nENDAIRCRAFTTYPE\n\n");
                                    sw.Write("STARTCARGO\n0\nENDCARGO\n\n");
                                    sw.Write("STARTMILITARY\n0\nENDMILITARY\n\n");
                                    sw.Write($"STARTRUNWAY\n{target.ToString()}\nENDRUNWAY\n\n");
                                    sw.Write("START_PARKING_CENTER\nNOSEWHEEL\nEND_PARKING_CENTER\n\n");
                                    sw.Write("STARTSTEERPOINTS\n");

                                    TaxiNode routeNode = fromNode.NearestVertex;                                    

                                    // Write the start point
                                    sw.Write($"{sp.Latitude * VortexMath.Rad2Deg} {sp.Longitude * VortexMath.Rad2Deg} -2 {sp.Bearing * VortexMath.Rad2Deg:0} 0 0 {sp.Name}\n");

                                    // Write Pushback node, allowing room for turn
                                    double addLat = 0;
                                    double addLon = 0;
                                    VortexMath.PointFrom(sp.PushBackLatitude, sp.PushBackLongitude, sp.Bearing, 0.020, ref addLat, ref addLon);
                                    sw.Write($"{addLat * VortexMath.Rad2Deg:0.00000000} {addLon * VortexMath.Rad2Deg:0.00000000} -1 -1 0 0 {routeNode.NameToTarget}\n");

                                    // See if we need to skip the first route node
                                    if (sp.AlternateAfterPushBack != null && sp.AlternateAfterPushBack == routeNode.PathToTarget)
                                    {
                                        // Our pushback point is better than the first point of the route
                                        routeNode = sp.AlternateAfterPushBack;
                                    }
                                    else if (VortexMath.DistancePyth(sp.PushBackLatitude, sp.PushBackLongitude, routeNode.Latitude, routeNode.Longitude) < 0.000000001)
                                    {
                                        // pushback node is the first route point
                                        routeNode = routeNode.PathToTarget;
                                    }

                                    // insert one more point here where the plane is pushed a little bit away from the next point
                                    if (routeNode != null)
                                    {
                                        double nextPushBearing = VortexMath.BearingRadians(routeNode.Latitude, routeNode.Longitude, sp.PushBackLatitude, sp.PushBackLongitude);
                                        VortexMath.PointFrom(sp.PushBackLatitude, sp.PushBackLongitude, nextPushBearing, 0.030, ref addLat, ref addLon);
                                        sw.Write($"{addLat * VortexMath.Rad2Deg:0.00000000} {addLon * VortexMath.Rad2Deg:0.00000000} 10 -1 0 0 {routeNode.NameToTarget}\n");
                                    }

                                    double lastBearing = VortexMath.BearingRadians(sp.PushBackLatitude, sp.PushBackLongitude, routeNode.Latitude, routeNode.Longitude);
                                    double lastLatitude = routeNode.Latitude;
                                    double lastLongitude = routeNode.Longitude;

                                    while (routeNode != null)
                                    {
                                        bool smoothed = false;
                                        // Check for corners that need to be smoothed
                                        if (routeNode.PathToTarget != null)
                                        {
                                            double turnAngle = 0;
                                            double nextBearing = VortexMath.BearingRadians(routeNode.Latitude, routeNode.Longitude, routeNode.PathToTarget.Latitude, routeNode.PathToTarget.Longitude);
                                            turnAngle = Math.Abs(((VortexMath.PI3 + lastBearing - nextBearing) % VortexMath.PI2) - Math.PI);
                                            if (turnAngle > VortexMath.PI025)
                                            {
                                                double distance = Math.Min(VortexMath.DistanceKM(lastLatitude, lastLongitude, routeNode.Latitude, routeNode.Longitude) - 0.0001, 0.025);
                                                VortexMath.PointFrom(routeNode.Latitude, routeNode.Longitude, lastBearing + VortexMath.PI, distance, ref addLat, ref addLon);
                                                sw.Write($"{addLat * VortexMath.Rad2Deg:0.00000000} {addLon * VortexMath.Rad2Deg:0.00000000} 8 -1 0 0 {routeNode.NameToTarget}\n");

                                                distance = Math.Min(VortexMath.DistanceKM(routeNode.Latitude, routeNode.Longitude, routeNode.PathToTarget.Latitude, routeNode.PathToTarget.Longitude) - 0.0001, 0.025);
                                                VortexMath.PointFrom(routeNode.Latitude, routeNode.Longitude, nextBearing, distance, ref addLat, ref addLon);
                                                sw.Write($"{addLat * VortexMath.Rad2Deg:0.00000000} {addLon * VortexMath.Rad2Deg:0.00000000} {speed} -1 0 0 {routeNode.NameToTarget}\n");

                                                smoothed = true;
                                            }
                                            lastBearing = nextBearing;
                                        }

                                        if (!smoothed)
                                        {
                                            if (routeNode.PathIsRunway)
                                            {
                                                sw.Write($"{routeNode.LatitudeString} {routeNode.LongitudeString} {speed} -1 {routeNode.NameToTarget} 1\n");
                                            }
                                            else
                                            {
                                                sw.Write($"{routeNode.LatitudeString} {routeNode.LongitudeString} {speed} -1 0 0 {routeNode.NameToTarget}\n");
                                            }
                                        }

                                        lastLatitude = routeNode.Latitude;
                                        lastLongitude = routeNode.Longitude;
                                        routeNode = routeNode.PathToTarget;
                                    }
                                    sw.Write("ENDSTEERPOINTS\n");
                                }
                            }
                        }
                    }


                    //if (target.NearestVertex.PathToTarget != null)
                    //{
                    //    List<TaxiNode> routeList = new List<TaxiNode>();
                    //    TaxiNode routeNode = target.NearestVertex;
                    //    while (routeNode != null)
                    //    {
                    //        routeList.Add(routeNode);
                    //        routeNode = routeNode.PathToTarget;
                    //    }
                    //    _cache[target.NearestVertex.Id][size] = routeList;
                    //}

                    //logElapsed($"calculations done for {target.ToString()} {size}");
                    //foreach (TargetNode startPoint in fromNodes)
                    //{
                    //    StartPoint sp = startPoint as StartPoint;

                    //    sb.Append($"{startPoint.ToString()}->{target.ToString()} Size:{size} Near: {startPoint.NearestVertex.Id}"
                    //              + $" Total Distance: {startPoint.NearestVertex.DistanceToTarget}\n");                        
                    //}

                    foreach (TaxiNode vtx in _vertices.Values)
                    {
                        vtx.DistanceToTarget = double.MaxValue;
                        vtx.PathToTarget = null;
                    }
                }
//                rtb.AppendText(sb.ToString());
            }
 //           logElapsed($"Cache Hits: {cacheHits}");
        }

        private void loadFile(string name)
        {
            _vertices = new Dictionary<ulong, TaxiNode>();
            _startPoints = new List<StartPoint>();
            _runways = new List<Runway>();
            _runwayEdges = new Dictionary<string, RunwayEdges>();


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
                    _runways.Add(r1);

                    Runway r2 = new Runway();
                    r2.Number = tokens[17];
                    r2.Latitude = double.Parse(tokens[18]) * VortexMath.Deg2Rad;
                    r2.Longitude = double.Parse(tokens[19]) * VortexMath.Deg2Rad;
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
                else if (lines[i].StartsWith("1300 "))
                {
                    string[] tokens = lines[i].Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens[5] != "helos") // ignore helos for now
                    {
                        StartPoint sp = new StartPoint();
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
            _vertices = _vertices.Values.Where(v => v.IncomingVertices.Count > 0).ToDictionary(v => v.Id);

            // With unneeded nodes gone, parse the lat/lon string and convert the values to radians
            foreach (TaxiNode v in _vertices.Values)
            {
                v.ComputeLonLat();
            }

            // Compute the lengths of each link (in arbitrary units
            // to avoid sin/cos and a find multiplications)
            foreach (TaxiNode v in _vertices.Values)
            {
                v.ComputeDistances();
            }

            // Damn you X plane for not requiring parking spots/gate to be linked
            // to the taxi route network. Why???????
            foreach (StartPoint sp in _startPoints)
            {
                sp.DetermineTaxiOutLocation(_vertices.Values);
            }

            // Find taxi nodes closest to runways
            foreach (Runway r in _runways)
            {
                double shortestDistance = double.MaxValue;

                foreach (TaxiNode vx in _vertices.Values.Where(v => v.IsRunwayEdge))
                {
                    double d = VortexMath.DistancePyth(vx.Latitude, vx.Longitude, r.Latitude, r.Longitude);
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
                rtb.AppendText($"Runway: {r.Number}, Nearest Node: {r.NearestVertex.Id}, Edges: {string.Join(", ", edgesKeys)}\n");

                string edgeKey = edgesKeys.FirstOrDefault();
                if (!string.IsNullOrEmpty(edgeKey))
                {
                    RunwayEdges rwe = _runwayEdges[edgeKey];
                    string chain = rwe.FindChainFrom(r.NearestVertex.Id);
                    rtb.AppendText($"{chain}\n");
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
