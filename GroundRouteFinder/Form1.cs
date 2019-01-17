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
        private Dictionary<ulong, Vertex> _vertices;
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

            // IEnumerable<TargetNode> targets = _runways.Select(r => r as TargetNode).Concat(_startPoints.Select(s => s as TargetNode));

            logElapsed("Arriving.....");
            process(_runways.ToList<TargetNode>(), _startPoints.ToList<TargetNode>());
            logElapsed("Departing.....");
            process(_startPoints.ToList<TargetNode>(), _runways.ToList<TargetNode>());


/*
            List<Vertex> _doneNodes = _vertices.Values.ToList();
            List<Vertex> _touchedNodes = new List<Vertex>();
            List<Vertex> _untouchedNodes = new List<Vertex>();



            foreach (TargetNode target in targets)
            {
                StringBuilder sb = new StringBuilder();
                for (int size = 0; size < Vertex.Sizes; size++)
                {
                    _untouchedNodes = _doneNodes;
                    _touchedNodes.Clear();
                    _doneNodes = new List<Vertex>();

                    Vertex v = target.NearestVertex;
                    v.DistanceToTarget = 0;

                    foreach (MeasuredVertex vto in v.IncommingVertices)
                    {
                        if (size > vto.MaxSize)
                            continue;

                        if (_untouchedNodes.Contains(vto.SourceVertex) && !_touchedNodes.Contains(vto.SourceVertex))
                        {
                            _untouchedNodes.Remove(vto.SourceVertex);
                            _touchedNodes.Add(vto.SourceVertex);
                        }

                        vto.SourceVertex.DistanceToTarget = vto.RelativeDistance;
                        vto.SourceVertex.PathToTarget = v;
                    }

                    _doneNodes.Add(v);
                    _untouchedNodes.Remove(v);

                    while (true)
                    {
                        if (_touchedNodes.Count() == 0)
                            break;

                        double min = _touchedNodes.Min(a => a.DistanceToTarget);
                        v = _touchedNodes.FirstOrDefault(a => a.DistanceToTarget == min);
                        if (v == null)
                            break;

                        foreach (MeasuredVertex vto in v.IncommingVertices)
                        {
                            if (size > vto.MaxSize)
                                continue;

                            // Try to force smaller aircraft to take their specific routes
                            double penalizedDistance = vto.RelativeDistance * (1.0 + 25.0 * (vto.MaxSize - size));

                            if (vto.SourceVertex.DistanceToTarget > (v.DistanceToTarget + vto.RelativeDistance))
                            {
                                if (_untouchedNodes.Contains(vto.SourceVertex) && !_touchedNodes.Contains(vto.SourceVertex))
                                {
                                    _untouchedNodes.Remove(vto.SourceVertex);
                                    _touchedNodes.Add(vto.SourceVertex);
                                }

                                vto.SourceVertex.DistanceToTarget = (v.DistanceToTarget + vto.RelativeDistance);
                                vto.SourceVertex.PathToTarget = v;
                            }
                        }

                        _touchedNodes.Remove(v);
                        _doneNodes.Add(v);
                    }

                    logElapsed($"calculations done for {target.ToString()} {size}");


                    foreach (StartPoint startPoint in _startPoints)
                    {
                        sb.Append($"{startPoint.Name}->{target.ToString()} Size:{size} Near: {startPoint.NearestVertex.Id} Total Distance: {startPoint.NearestVertex.DistanceToTarget}\n");
                        break;
                    }

                    foreach (Vertex vtx in _vertices.Values)
                    {
                        vtx.DistanceToTarget = double.MaxValue;
                        vtx.PathToTarget = null;
                    }                    
                }
                rtb.AppendText(sb.ToString());
                
            }
*/
            logElapsed("processing done");
        }

        private void process(List<TargetNode> fromNodes, List<TargetNode> toNodes)
        {
            List<Vertex> doneNodes = _vertices.Values.ToList();
            List<Vertex> touchedNodes = new List<Vertex>();
            List<Vertex> untouchedNodes = new List<Vertex>();

            foreach (TargetNode target in toNodes)
            {
                StringBuilder sb = new StringBuilder();
                for (int size = 0; size < Vertex.Sizes; size++)
                {
                    // Could check to see if this really helps performance :D
                    doneNodes.AddRange(touchedNodes);
                    doneNodes.AddRange(untouchedNodes);
                    untouchedNodes = doneNodes;

                    touchedNodes = new List<Vertex>();
                    doneNodes = new List<Vertex>();

                    Vertex v = target.NearestVertex;
                    v.DistanceToTarget = 0;

                    foreach (MeasuredVertex vto in v.IncommingVertices)
                    {
                        if (size > vto.MaxSize)
                            continue;

                        if (untouchedNodes.Contains(vto.SourceVertex) && !touchedNodes.Contains(vto.SourceVertex))
                        {
                            untouchedNodes.Remove(vto.SourceVertex);
                            touchedNodes.Add(vto.SourceVertex);
                        }

                        vto.SourceVertex.DistanceToTarget = vto.RelativeDistance;
                        vto.SourceVertex.PathToTarget = v;
                    }

                    doneNodes.Add(v);
                    untouchedNodes.Remove(v);

                    while (true)
                    {
                        if (touchedNodes.Count() == 0)
                            break;

                        double min = touchedNodes.Min(a => a.DistanceToTarget);
                        v = touchedNodes.FirstOrDefault(a => a.DistanceToTarget == min);
                        if (v == null)
                            break;

                        foreach (MeasuredVertex vto in v.IncommingVertices)
                        {
                            if (size > vto.MaxSize)
                                continue;

                            // Try to force smaller aircraft to take their specific routes
                            double penalizedDistance = vto.RelativeDistance * (1.0 + 2.0 * (vto.MaxSize - size));

                            if (vto.SourceVertex.DistanceToTarget > (v.DistanceToTarget + penalizedDistance))
                            {
                                if (untouchedNodes.Contains(vto.SourceVertex) && !touchedNodes.Contains(vto.SourceVertex))
                                {
                                    untouchedNodes.Remove(vto.SourceVertex);
                                    touchedNodes.Add(vto.SourceVertex);
                                }

                                vto.SourceVertex.DistanceToTarget = (v.DistanceToTarget + penalizedDistance);
                                vto.SourceVertex.PathToTarget = v;
                            }
                        }

                        touchedNodes.Remove(v);
                        doneNodes.Add(v);
                    }

                    logElapsed($"calculations done for {target.ToString()} {size}");


                    foreach (TargetNode startPoint in fromNodes)
                    {
                        StartPoint sp = startPoint as StartPoint;
                        if (sp != null)
                        {
                            sp.FindTaxiOutPoint();
                        }

                        sb.Append($"{startPoint.ToString()}->{target.ToString()} Size:{size} Near: {startPoint.NearestVertex.Id}"
                                  + $" Total Distance: {startPoint.NearestVertex.DistanceToTarget}\n");
                        
                    }

                    foreach (Vertex vtx in _vertices.Values)
                    {
                        vtx.DistanceToTarget = double.MaxValue;
                        vtx.PathToTarget = null;
                    }
                }
                rtb.AppendText(sb.ToString());
                break;
            }

        }

        private void loadFile(string name)
        {
            _vertices = new Dictionary<ulong, Vertex>();
            _startPoints = new List<StartPoint>();
            _runways = new List<Runway>();
            _runwayEdges = new Dictionary<string, RunwayEdges>();


            string[] lines = File.ReadAllLines(name);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("100 "))
                {
                    string[] tokens = lines[i].Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
                    Runway r = new Runway();
                    r.Number = tokens[8];
                    r.ActualLatitude = double.Parse(tokens[9]) * (Math.PI / 180.0);
                    r.ActualLongitude = double.Parse(tokens[10]) * (Math.PI / 180.0);
                    _runways.Add(r);

                    r = new Runway();
                    r.Number = tokens[17];
                    r.ActualLatitude = double.Parse(tokens[18]) * (Math.PI / 180.0);
                    r.ActualLongitude = double.Parse(tokens[19]) * (Math.PI / 180.0);
                    _runways.Add(r);
                }
                else if (lines[i].StartsWith("1201 "))
                {
                    string[] tokens = lines[i].Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
                    ulong id = ulong.Parse(tokens[4]);
                    _vertices[id] = new Vertex(id, tokens[1], tokens[2]);
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

                    _vertices[vb].AddEdgeFrom(_vertices[va], maxSize, isRunway);
                    if (tokens[3][0] == 't')
                    {
                        _vertices[va].AddEdgeFrom(_vertices[vb], maxSize, isRunway);
                    }
                }
                else if (lines[i].StartsWith("1300 "))
                {
                    string[] tokens = lines[i].Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
                    StartPoint sp = new StartPoint();
                    sp.ActualLatitude = double.Parse(tokens[1]) * (Math.PI / 180.0);
                    sp.ActualLongitude = double.Parse(tokens[2]) * (Math.PI / 180.0);
                    sp.Bearing = ((double.Parse(tokens[3]) + 540) * (Math.PI / 180.0)) % (Math.PI * 2.0) - Math.PI;
                    sp.Type = tokens[4];
                    sp.Jets = tokens[5];
                    sp.Name = string.Join(" ", tokens.Skip(6));
                    _startPoints.Add(sp);
                }
            }
        }

        private void preprocess()
        {
            // NEXT: EN81 is too far away

            _vertices = _vertices.Values.Where(v => v.IncommingVertices.Count > 0).ToDictionary(v => v.Id);

            foreach (Vertex v in _vertices.Values)
            {
                v.ComputeLonLat();
            }

            foreach (Vertex v in _vertices.Values)
            {
                v.ComputeDistances();
            }

            StreamWriter sblues = File.CreateText("D:\\start_pushpoints.csv");
            sblues.WriteLine("Lat,Lon,Name");


            foreach (StartPoint sp in _startPoints)
            {
                double shortestDistance = double.MaxValue;
                double bestPushBackLatitude = 0;
                double bestPushBackLongitude = 0;

                //if (sp.Name == "H11")
                //{
                //    int k = 7;
                //}

                double spBearing = sp.Type == "gate" ? sp.Bearing : (sp.Bearing + Math.PI);
                if (spBearing > Math.PI)
                    spBearing -= (2.0 * Math.PI);

                foreach (Vertex vx in _vertices.Values)
                {
                    vx.TemporaryDistance = vx.CrudeRelativeDistanceEstimate(sp.ActualLatitude, sp.ActualLongitude);
                    //double d = vx.CrudeRelativeDistanceEstimate(sp.ActualLatitude, sp.ActualLongitude);
                    //if (d < shortestDistance)
                    //{
                    //    shortestDistance = d;
                    //    sp.NearestVertex = vx;
                    //}
                }

                var selected = _vertices.Values.OrderBy(v => v.TemporaryDistance).Take(25);
                selected = selected.Where(v => /*v.DistanceKM(sp.ActualLatitude, sp.ActualLongitude) < 0.250 && */ Math.Abs(spBearing - StartPoint.ComputeBearing(v.Latitude, v.Longitude, sp.ActualLatitude, sp.ActualLongitude)) < 0.5 * Math.PI);
                int fail = 0;
                foreach (Vertex v in selected)
                {
                    foreach (var incomming in v.IncommingVertices)
                    {
                        if (!incomming.Bearing.HasValue)
                        {
                            incomming.Bearing = StartPoint.ComputeBearing(incomming.SourceVertex.Latitude, incomming.SourceVertex.Longitude, v.Latitude, v.Longitude);
                        }

                        //double dBearing = (incomming.Bearing.Value - spBearing) % (Math.PI * 2.0);
                        //if (dBearing < 0.5 * Math.PI || dBearing > 1.5 * Math.PI)
                        //    continue;

                        double pushBackLatitude = 0;
                        double pushBackLongitude = 0;
                        if (!StartPoint.Intersection(sp.ActualLatitude, sp.ActualLongitude, spBearing,
                                                    incomming.SourceVertex.Latitude, incomming.SourceVertex.Longitude, incomming.Bearing.Value,
                                                    ref pushBackLatitude, ref pushBackLongitude))
                        {
                            if (!StartPoint.Intersection(sp.ActualLatitude, sp.ActualLongitude, spBearing, 
                                                         incomming.SourceVertex.Latitude, incomming.SourceVertex.Longitude, incomming.Bearing.Value + Math.PI,
                                                         ref pushBackLatitude, ref pushBackLongitude))
                            {
                                //if (sp.Name == "H11")
                                //{
                                //    fail++;
                                //    sblues.WriteLine($"{sp.ActualLatitude * 180.0 / Math.PI},{sp.ActualLongitude * 180.0 / Math.PI},{fail}-{sp.Name}");
                                //    sblues.WriteLine($"{v.Latitude * 180.0 / Math.PI},{v.Longitude * 180.0 / Math.PI},{fail}-{"dest"}");
                                //    sblues.WriteLine($"{incomming.SourceVertex.Latitude * 180.0 / Math.PI},{incomming.SourceVertex.Longitude * 180.0 / Math.PI},{fail}-{"src"}");
                                //}
                                continue;
                            }
                        }

                        double pushDistance = Vertex.CrudeRelativeDistanceEstimate(v.Latitude, v.Longitude, pushBackLatitude, pushBackLongitude);
                        if (pushDistance > Math.PI)
                        {
                            pushBackLatitude = -pushBackLatitude;
                            pushBackLongitude += Math.PI;
                            pushDistance = Vertex.CrudeRelativeDistanceEstimate(v.Latitude, v.Longitude, pushBackLatitude, pushBackLongitude);
                        }

                        if (pushDistance < shortestDistance)
                        {
                            bestPushBackLatitude = pushBackLatitude;
                            bestPushBackLongitude = pushBackLongitude;
                            shortestDistance = pushDistance;

                            //if (sp.Name == "H11")
                            //{
                            //    sblues.WriteLine($"{v.Latitude * 180.0 / Math.PI},{v.Longitude * 180.0 / Math.PI},{"better"}-{"dest"}");
                            //    sblues.WriteLine($"{incomming.SourceVertex.Latitude * 180.0 / Math.PI},{incomming.SourceVertex.Longitude * 180.0 / Math.PI},{"better"}-{"src"}");
                            //}
                        }
                    }
                }

                //if (sp.Name == "H11")
                //{
                //    foreach (Vertex v in selected)
                //    {
                //        sblues.WriteLine($"{v.Latitude * 180.0 / Math.PI},{v.Longitude * 180.0 / Math.PI},{v.Name},{spBearing - StartPoint.ComputeBearing(v.Latitude, v.Longitude, sp.ActualLatitude, sp.ActualLongitude)}");
                //    }
                //    if (shortestDistance < double.MaxValue)
                //    {
                //        sblues.WriteLine($"{bestPushBackLatitude * 180.0 / Math.PI},{bestPushBackLongitude * 180.0 / Math.PI},{sp.Name}");
                //    }
                //}

                if (shortestDistance < double.MaxValue)
                {
                    sblues.WriteLine($"{bestPushBackLatitude * 180.0 / Math.PI},{bestPushBackLongitude * 180.0 / Math.PI},{sp.Name}");
                }
                else
                {
                    sblues.WriteLine($"Failure: {sp.Name}, candidates: {selected.Count()}");
                }
            }

            sblues.Close();

            foreach (Runway r in _runways)
            {
                double shortestDistance = double.MaxValue;

                foreach (Vertex vx in _vertices.Values.Where(v => v.IsRunwayEdge))
                {
                    double d = vx.CrudeRelativeDistanceEstimate(r.ActualLatitude, r.ActualLongitude);
                    if (d < shortestDistance)
                    {
                        shortestDistance = d;
                        r.NearestVertex = vx;
                    }
                }
            }

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
