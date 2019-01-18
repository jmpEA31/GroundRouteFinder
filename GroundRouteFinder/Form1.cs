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

            logElapsed("Arriving.....");
            process(_runways.ToList<TargetNode>(), _startPoints.ToList<TargetNode>());
            logElapsed("Departing.....");
            process(_startPoints.ToList<TargetNode>(), _runways.ToList<TargetNode>());
            logElapsed("processing done");
        }

        private void process(List<TargetNode> fromNodes, List<TargetNode> toNodes)
        {
            List<TaxiNode> doneNodes = _vertices.Values.ToList();
            List<TaxiNode> touchedNodes = new List<TaxiNode>();
            List<TaxiNode> untouchedNodes = new List<TaxiNode>();

            foreach (TargetNode target in toNodes)
            {
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

                        foreach (MeasuredNode vto in v.IncomingVertices)
                        {
                            if (size > vto.MaxSize)
                                continue;

                            // Try to force smaller aircraft to take their specific routes
                            double penalizedDistance = vto.RelativeDistance * (1.0 + 2.0 * (vto.MaxSize - size));

                            if (vto.SourceNode.DistanceToTarget > (v.DistanceToTarget + penalizedDistance))
                            {
                                if (untouchedNodes.Contains(vto.SourceNode) && !touchedNodes.Contains(vto.SourceNode))
                                {
                                    untouchedNodes.Remove(vto.SourceNode);
                                    touchedNodes.Add(vto.SourceNode);
                                }

                                vto.SourceNode.DistanceToTarget = (v.DistanceToTarget + penalizedDistance);
                                vto.SourceNode.PathToTarget = v;
                            }
                        }

                        touchedNodes.Remove(v);
                        doneNodes.Add(v);
                    }

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
                    Runway r = new Runway();
                    r.Number = tokens[8];
                    r.Latitude = double.Parse(tokens[9]) * VortexMath.Deg2Rad;
                    r.Longitude = double.Parse(tokens[10]) * VortexMath.Deg2Rad;
                    _runways.Add(r);

                    r = new Runway();
                    r.Number = tokens[17];
                    r.Latitude = double.Parse(tokens[18]) * VortexMath.Deg2Rad;
                    r.Longitude = double.Parse(tokens[19]) * VortexMath.Deg2Rad;
                    _runways.Add(r);
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

                    _vertices[vb].AddEdgeFrom(_vertices[va], maxSize, isRunway);
                    if (tokens[3][0] == 't')
                    {
                        _vertices[va].AddEdgeFrom(_vertices[vb], maxSize, isRunway);
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
