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

                    foreach (MeasuredVertex vto in v.IncomingVertices)
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

                        foreach (MeasuredVertex vto in v.IncomingVertices)
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
                    if (tokens[5] != "helos") // ignore helos for now
                    {
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
        }

        private void preprocess()
        {
            // Filter out nodes with links (probably nodes for the vehicle network)
            _vertices = _vertices.Values.Where(v => v.IncomingVertices.Count > 0).ToDictionary(v => v.Id);

            // With unneeded nodes gone, parse the lat/lon string and convert the values to radians
            foreach (Vertex v in _vertices.Values)
            {
                v.ComputeLonLat();
            }

            // Compute the lengths of each link (in arbitrary units
            // to avoid sin/cos and a find multiplications)
            foreach (Vertex v in _vertices.Values)
            {
                v.ComputeDistances();
            }

            // Damn you X plane for not requiring parking spots/gate to be linked
            // to the taxi route network. Why???????
            foreach (StartPoint sp in _startPoints)
            {
                double shortestDistance = double.MaxValue;
                double bestPushBackLatitude = 0;
                double bestPushBackLongitude = 0;
                Vertex firstAfterPush = null;
                Vertex alternateAfterPush = null;

                // For gates use the indicated bearings (push back), for others add 180 degrees for straight out
                // Then convert to -180...180 range
                double spBearing = sp.Type == "gate" ? sp.Bearing : (sp.Bearing + Math.PI);
                if (spBearing > Math.PI)
                    spBearing -= (2.0 * Math.PI);

                // Compute the distance (arbitrary units) from each taxi node to the start location
                foreach (Vertex vx in _vertices.Values)
                {
                    vx.TemporaryDistance = vx.CrudeRelativeDistanceEstimate(sp.ActualLatitude, sp.ActualLongitude);
                }

                // Select the 25 nearest, then from those select only the ones that are in the 180 degree arc of the direction
                // we intend to move in from the startpoint
                // todo: make both 25 and 180 parameters
                IEnumerable<Vertex> selected = _vertices.Values.OrderBy(v => v.TemporaryDistance).Take(25);
                selected = selected.Where(v =>Math.Abs(spBearing - StartPoint.ComputeBearing(v.Latitude, v.Longitude, sp.ActualLatitude, sp.ActualLongitude)) < 0.5 * Math.PI);

                // For each qualifying node
                foreach (Vertex v in selected)
                {
                    // Look at each link coming into it from other nodes
                    foreach (MeasuredVertex incoming in v.IncomingVertices)
                    {
                        // Compute the bearing of the link if it has not been calculated yet
                        // todo: how much time do I gain here? Might be easier, clearer and nearly as fast to compute all of them while adding them during the file read
                        if (!incoming.Bearing.HasValue)
                        {
                            incoming.Bearing = StartPoint.ComputeBearing(incoming.SourceVertex.Latitude, incoming.SourceVertex.Longitude, v.Latitude, v.Longitude);
                        }

                        double pushBackLatitude = 0;
                        double pushBackLongitude = 0;

                        // Now find where the 'start point outgoing line' intersects with the taxi link we are currently checking
                        if (!StartPoint.Intersection(sp.ActualLatitude, sp.ActualLongitude, spBearing,
                                                    incoming.SourceVertex.Latitude, incoming.SourceVertex.Longitude, incoming.Bearing.Value,
                                                    ref pushBackLatitude, ref pushBackLongitude))
                        {
                            // If computation fails, try again but now with the link in the other direction.
                            // Ignoring one way links here, I just want a push back target for now that's close to A link.
                            // todo: is this needed? Will it work if the first check failed???
                            if (!StartPoint.Intersection(sp.ActualLatitude, sp.ActualLongitude, spBearing,
                                                         incoming.SourceVertex.Latitude, incoming.SourceVertex.Longitude, incoming.Bearing.Value + Math.PI,
                                                         ref pushBackLatitude, ref pushBackLongitude))
                            {
                                // Lines might be parallel, can't find intersection, skip
                                continue;
                            }
                        }

                        // Great Circles cross twice, if we found the one on the back of the earth, convert it to the
                        // one on the airport
                        // Todo: check might fail for airports on the -180/+180 longitude line
                        if (Math.Abs(pushBackLongitude - sp.ActualLongitude) > 0.25 * Math.PI)
                        {
                            pushBackLatitude = -pushBackLatitude;
                            pushBackLongitude += Math.PI;
                        }

                        // To find the best spot we must know if the found intersection is actually
                        // on the link or if it is somewhere outside the actual link. These are 
                        // still usefull in some cases
                        bool foundTargetIsOutsideSegment = false;

                        // Todo: check might fail for airports on the -180/+180 longitude line
                        if (pushBackLatitude - incoming.SourceVertex.Latitude > 0)
                        {
                            if (v.Latitude - pushBackLatitude <= 0)
                                foundTargetIsOutsideSegment = true;
                        }
                        else if (v.Latitude - pushBackLatitude > 0)
                            foundTargetIsOutsideSegment = true;

                        if (pushBackLongitude - incoming.SourceVertex.Longitude > 0)
                        {
                            if (v.Longitude - pushBackLongitude <= 0)
                                foundTargetIsOutsideSegment = true;
                        }
                        else if (v.Longitude - pushBackLongitude > 0)
                            foundTargetIsOutsideSegment = true;

                        // Ignore links where the taxiout line intercepts at too sharp of an angle if it is 
                        // also outside the actual link.
                        // todo: Maybe ignore these links right away, saves a lot of calculations
                        double interceptAngleSharpness = Math.Abs(0.5 * Math.PI - Math.Abs((sp.Bearing - incoming.Bearing.Value) % Math.PI))/Math.PI;
                        if (foundTargetIsOutsideSegment && interceptAngleSharpness > 0.4)
                        {
                            continue;
                        }

                        // for the found location keep track of the distance to it from the start point
                        // also keep track of the distances to both nodes of the link we are inspecting now
                        double pushDistance = 0.0;
                        double distanceSource = Vertex.CrudeRelativeDistanceEstimate(incoming.SourceVertex.Latitude, incoming.SourceVertex.Longitude, pushBackLatitude, pushBackLongitude);
                        double distanceDest = Vertex.CrudeRelativeDistanceEstimate(v.Latitude, v.Longitude, pushBackLatitude, pushBackLongitude);

                        // If the found point is outside the link, add the distance to the nearest node of
                        // the link time 2 as a penalty to the actual distance. This prevents pushback point
                        // candidates that sneak up on the start because of a slight angle in remote link
                        // from being accepted as best.
                        Vertex nearestVertexIfPushBackOutsideSegment = null;
                        if (foundTargetIsOutsideSegment)
                        {
                            if (distanceSource < distanceDest)
                            {
                                pushDistance = distanceSource * 2.0;
                                nearestVertexIfPushBackOutsideSegment = incoming.SourceVertex;
                            }
                            else
                            {
                                pushDistance = distanceDest * 2.0;
                                nearestVertexIfPushBackOutsideSegment = v;
                            }
                        }

                        // How far is the candidate from the start point?
                        pushDistance += Vertex.CrudeRelativeDistanceEstimate(sp.ActualLatitude, sp.ActualLongitude, pushBackLatitude, pushBackLongitude);

                        // See if it is a better candidate
                        if (pushDistance < shortestDistance)
                        {
                            bestPushBackLatitude = pushBackLatitude;
                            bestPushBackLongitude = pushBackLongitude;
                            shortestDistance = pushDistance;

                            // Setting things up for the path calculation that will follow later
                            if (foundTargetIsOutsideSegment)
                            {
                                // The taxi out route will start with a push to the best candidate
                                // Then move to the 'firstAfterPush' node and from there follow
                                // the 'shortest' path to the runway
                                firstAfterPush = nearestVertexIfPushBackOutsideSegment;
                                alternateAfterPush = null;
                            }
                            else
                            {
                                // The taxi out route will start with a push to the best candidate
                                // Then, if the second node in the find 'shortest' path is the alternate
                                // the first point will be skipped. If the second point is not the alternate,
                                // the 'firstAfterPush' will be the first indeed and after that the found
                                // route will be followed.
                                if (distanceSource < distanceDest)
                                {
                                    firstAfterPush = incoming.SourceVertex;
                                    alternateAfterPush = v;
                                }
                                else
                                {
                                    firstAfterPush = v;
                                    alternateAfterPush = incoming.SourceVertex;
                                }
                            }
                        }
                    }
                }

                // All candiates have been considered, post processing the winner:
                if (shortestDistance < double.MaxValue)
                {
                    // If there is one, check if it is not too far away from the start. This catches cases where
                    // a gate at the end of an apron with heading parallel to the apron entry would get a best
                    // target on the taxiway outside the apron.
                    double actualDistance = Vertex.DistanceKM(sp.ActualLatitude, sp.ActualLongitude, bestPushBackLatitude, bestPushBackLongitude);
                    if (actualDistance > 0.25)
                    {
                        // Fix this by pushing to the end point of the entry link
                        // (If that is actually the nearest node to the parking, but alas...
                        //  this is the default WT3 behaviour anyway)
                        sp.NearestVertex = selected.First();
                        sp.AlternateAfterPushBack = null;
                        sp.PushBackLatitude = sp.NearestVertex.Latitude;
                        sp.PushBackLongitude = sp.NearestVertex.Longitude;
                    }
                    else
                    {
                        // Store the results in the startpoint
                        sp.PushBackLatitude = bestPushBackLatitude;
                        sp.PushBackLongitude = bestPushBackLongitude;
                        sp.NearestVertex = firstAfterPush;
                        sp.AlternateAfterPushBack = alternateAfterPush;
                    }
                }
                else
                {
                    // Crude fallback to defautl WT behavoit if nothing was found.
                    sp.NearestVertex = selected.First();
                    sp.AlternateAfterPushBack = null;
                    sp.PushBackLatitude = sp.NearestVertex.Latitude;
                    sp.PushBackLongitude = sp.NearestVertex.Longitude;
                }
            }

            // Find taxi nodes closest to runways
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
