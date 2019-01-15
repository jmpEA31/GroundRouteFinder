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

            loadFile("..\\..\\..\\..\\LFPG_Scenery_Pack\\LFPG_Scenery_Pack\\Earth nav data\\apt.dat");
            logElapsed("loading done");
            preprocess();
            logElapsed("preprocessing done");

            IEnumerable<TargetNode> targets = _runways.Select(r => r as TargetNode).Concat(_startPoints.Select(s => s as TargetNode));

            foreach (TargetNode target in targets)
            {
                Vertex v = target.NearestVertex;
                for (int i = 0; i < Vertex.Sizes; i++)
                {
                    v.DistanceToTarget[i] = 0;
                    v.Done[i] = true;
                }

                foreach (MeasuredVertex vto in v.IncommingVertices)
                {
                    for (int i = 0; i <= vto.MaxSize; i++)
                    {
                        vto.SourceVertex.DistanceToTarget[i] = vto.RelativeDistance;
                        vto.SourceVertex.PathToTarget[i] = v;
                    }
                }


                int procced = 0;
                StringBuilder sb = new StringBuilder();
                for (int size = 0; size < Vertex.Sizes; size++)
                {
                    while (_vertices.Count > 0)
                    {
                        v = _vertices.Values.FirstOrDefault(a => a.PathToTarget[size] != null && !a.Done[size]);
                        if (v == null)
                            break;

                        //var kvp = _vertices.FirstOrDefault(a => a.Value.PathToTarget[0] != null && !a.Value.Done);
                        //if (kvp.Value == null)
                        //    break;
                        //else
                        //    v = kvp.Value;
                        foreach (MeasuredVertex vto in v.IncommingVertices)
                        {
                            if (vto.MaxSize < size)
                                continue;

                            //                        for (int i = 0; i <= vto.MaxSize; i++)
                            //                        {
                            if (v.PathToTarget[size] == null)
                                continue;

                            if (vto.SourceVertex.DistanceToTarget[size] > (v.DistanceToTarget[size] + vto.RelativeDistance))
                            {
                                vto.SourceVertex.DistanceToTarget[size] = (v.DistanceToTarget[size] + vto.RelativeDistance);
                                vto.SourceVertex.PathToTarget[size] = v;
                            }
                            //}
                        }
                        v.Done[size] = true;
                        procced++;
                    }

                //    logElapsed($"calculations done for {target.ToString()} {size}");
                }
                //foreach (StartPoint startPoint in _startPoints)
                //{
                //    sb.AppendFormat("{0} Near: {1}\n", startPoint.Name, startPoint.NearestVertex.Id);
                //    for (int s = 0; s < 6; s++)
                //    {
                //        if (startPoint.NearestVertex.PathToTarget[s] == null)
                //        {
                //            sb.AppendFormat($"No {s} Path\n");
                //        }
                //        else
                //        {
                //            Vertex vx = startPoint.NearestVertex;
                //            while (vx.PathToTarget[s] != null)
                //            {
                //                sb.AppendFormat("{0}(", vx.Id);
                //                //for (int sz = 0; sz < 6; sz++)
                //                //{
                //                //    sb.AppendFormat("{0} ", vx.PathToTarget[sz] == null ? "x" : vx.PathToTarget[sz].Id.ToString());
                //                //}
                //                sb.Append(")->");
                //                vx = vx.PathToTarget[s];
                //            }
                //            sb.AppendFormat(">{0}\n", vx.Id);
                //        }
                //    }
                //    break;
                //}
                //rtb.AppendText(sb.ToString());
                //break;

                foreach (Vertex vtx in _vertices.Values)
                {
                    for (int i = 0; i < Vertex.Sizes; i++)
                    {
                        vtx.DistanceToTarget[i] = double.MaxValue;
                        vtx.PathToTarget[i] = null;
                        vtx.Done[i] = false;
                    }
                }
            }
            logElapsed("processing done");
        }

        private void loadFile(string name)
        {
            _vertices = new Dictionary<ulong, Vertex>();
            _startPoints = new List<StartPoint>();
            _runways = new List<Runway>();

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
                }
                else if (lines[i].StartsWith("1202 "))
                {
                    string[] tokens = lines[i].Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
                    ulong va = ulong.Parse(tokens[1]);
                    ulong vb = ulong.Parse(tokens[2]);
                    int maxSize = 5;
                    if (tokens[4][0] == 't')
                        maxSize = (int)(tokens[4][8]-'A');

                    _vertices[vb].AddEdgeFrom(_vertices[va], maxSize);
                    if (tokens[3][0] == 't')
                    {
                        _vertices[va].AddEdgeFrom(_vertices[vb], maxSize);
                    }
                }
                else if (lines[i].StartsWith("1300 "))
                {
                    string[] tokens = lines[i].Split(_splitters, StringSplitOptions.RemoveEmptyEntries);
                    StartPoint sp = new StartPoint();
                    sp.ActualLatitude = double.Parse(tokens[1]) * (Math.PI / 180.0);
                    sp.ActualLongitude = double.Parse(tokens[2]) * (Math.PI / 180.0);
                    sp.Type = tokens[4];
                    sp.Jets = tokens[5];
                    sp.Name = string.Join(" ", tokens.Skip(6));
                    _startPoints.Add(sp);
                }
            }
        }

        private void preprocess()
        {
            _vertices = _vertices.Values.Where(v => v.IncommingVertices.Count > 0).ToDictionary(v => v.Id);
            foreach (Vertex v in _vertices.Values)
            {
                v.ComputeLonLat();
            }

            foreach (Vertex v in _vertices.Values)
            {
                v.ComputeDistances();
            }

            foreach (StartPoint sp in _startPoints)
            {
                double shortestDistance = double.MaxValue;

                foreach (Vertex vx in _vertices.Values)
                {
                    double d = vx.CrudeRelativeDistanceEstimate(sp.ActualLatitude, sp.ActualLongitude);
                    if (d < shortestDistance)
                    {
                        shortestDistance = d;
                        sp.NearestVertex = vx;
                    }
                }
            }

            foreach (Runway r in _runways)
            {
                double shortestDistance = double.MaxValue;

                foreach (Vertex vx in _vertices.Values)
                {
                    double d = vx.CrudeRelativeDistanceEstimate(r.ActualLatitude, r.ActualLongitude);
                    if (d < shortestDistance)
                    {
                        shortestDistance = d;
                        r.NearestVertex = vx;
                    }
                }
            }

        }
    }
}
