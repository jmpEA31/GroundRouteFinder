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
        //private Dictionary<ulong, Vertex> _processedVertices;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DateTime start = DateTime.Now;
            rtb.Clear();
            char[] splitters = { ' ' };

            _vertices = new Dictionary<ulong, Vertex>();
            _startPoints = new List<StartPoint>();
            //_processedVertices = new Dictionary<ulong, Vertex>();

            string [] lines = File.ReadAllLines("D:\\SteamLibrary\\steamapps\\common\\X-Plane 11\\Custom Scenery\\Aerosoft - EGLL Heathrow\\Earth nav data\\apt.dat");
            int edges = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("120"))
                {
                    string[] tokens = lines[i].Split(splitters, StringSplitOptions.RemoveEmptyEntries);
                    switch (tokens[0])
                    {
                        case "1201":
                            ulong id = ulong.Parse(tokens[4]);
                            _vertices[id] = new Vertex(id, double.Parse(tokens[1]), double.Parse(tokens[2]));
                            break;
                        case "1202":
                            ulong va = ulong.Parse(tokens[1]);
                            ulong vb = ulong.Parse(tokens[2]);
                            _vertices[vb].AddEdgeFrom(_vertices[va]);
                            edges++;

                            if (tokens[3][0] == 't')
                            {
                                _vertices[va].AddEdgeFrom(_vertices[vb]);
                                edges++;
                            }

                            break;
                        default:
                            break;
                    }
                }
                else if (lines[i].StartsWith("130"))
                {
                    string[] tokens = lines[i].Split(splitters, StringSplitOptions.RemoveEmptyEntries);
                    switch (tokens[0])
                    {
                        case "1300":
                            StartPoint sp = new StartPoint();
                            sp.Latitude = double.Parse(tokens[1]) * (Math.PI / 180.0);
                            sp.Longitude = double.Parse(tokens[2]) * (Math.PI / 180.0); 
                            sp.Type = tokens[4]; 
                            sp.Jets = tokens[5];
                            sp.Name = tokens[6];
                            _startPoints.Add(sp);
                            break;
                        default:
                            break;
                    }
                }
            }

            foreach (StartPoint sp in _startPoints)
            {
                double shortestDistance = double.MaxValue;

                foreach (Vertex vx in _vertices.Values.Where(vtx => vtx.IncommingVertices.Count > 0))
                {
                    double d = vx.Distance(sp.Latitude, sp.Longitude);
                    if (d < shortestDistance)
                    {
                        shortestDistance = d;
                        sp.NearestVertex = vx;
                    }
                }
            }

            Vertex v = _vertices[459];
            v.DistanceToTarget = 0;

            // _vertices.Remove(0);

            int pathsInited = 0;

            foreach (MeasuredVertex vto in v.IncommingVertices)
            {
                vto.vertex.DistanceToTarget = vto.distance;
                vto.vertex.PathToTarget = v;
                pathsInited++;
            }

            v.Done = true;
            //_processedVertices[0] = v;

            int procced = 0;
            while (_vertices.Count > 0)
            {
                var kvp = _vertices.FirstOrDefault(a => a.Value.PathToTarget != null && !a.Value.Done);
                if (kvp.Value == null)
                    break;
                else
                    v = kvp.Value;

                procced++;

                foreach (MeasuredVertex vto in v.IncommingVertices)
                {
                    if (vto.vertex.DistanceToTarget > (v.DistanceToTarget + vto.distance))
                    {
                        vto.vertex.DistanceToTarget = (v.DistanceToTarget + vto.distance);
                        if (vto.vertex.PathToTarget == null)
                            pathsInited++;

                        vto.vertex.PathToTarget = v;
                    }
                }
//                _vertices.Remove(v.Id);
                v.Done = true;
//                _processedVertices[v.Id] = v;
            }
            var kvpk = _vertices.FirstOrDefault(a => !a.Value.Done);

            rtb.AppendText($"{(DateTime.Now - start).TotalSeconds} Done\n");

            StringBuilder sb = new StringBuilder();

            foreach (StartPoint startPoint in _startPoints)
            {
                sb.AppendFormat("{0} Near: {1} ", startPoint.Name, startPoint.NearestVertex.Id);
                if (startPoint.NearestVertex.PathToTarget == null)
                {
                    sb.AppendFormat("No Path\n");
                }
                else
                {
                    v = startPoint.NearestVertex;
                    while (v.PathToTarget != null)
                    {
                        sb.AppendFormat("{0}->", v.Id);
                        v = v.PathToTarget;
                    }
                    sb.AppendFormat("\n");
                }
            }

            rtb.AppendText(sb.ToString());
        }
    }
}
