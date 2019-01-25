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
        private List<TaxiEdge> _edges;

        private static char[] _splitters = { ' ' };

        public Airport()
        {
        }

        public void Load(string name)
        {
            _nodeDict = new Dictionary<ulong, TaxiNode>();
            _parkings = new List<Parking>();
            _runways = new List<Runway>();
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

            // Compute distance and bearing of each edge
            foreach (TaxiEdge edge in _edges)
            {
                edge.Compute();
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
                Console.WriteLine($"-------------{r.Designator}----------------");
                r.Analyze(_taxiNodes, _edges);
            }
        }

        public void FindInboundRoutes()
        {
            foreach (Parking parking in _parkings)
            {
                InboundResults ir = new InboundResults(_edges, parking);
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
                ir.WriteRoutes();
            }
        }

        public void FindOutboundRoutes()
        {
            // for each runway
            foreach (Runway runway in _runways)
            {
                //_resultCache = new Dictionary<Parking, Dictionary<TaxiNode, ResultRoute>>();

                // for each takeoff spot
                foreach (RunwayTakeOffSpot takeoffSpot in runway.TakeOffSpots)
                {
                    OutboundResults or = new OutboundResults(_edges, runway);
                    // for each size
                    for (int size = TaxiNode.Sizes - 1; size >= 0; size--)
                    {
                        // find shortest path from each parking to each takeoff spot considering each entrypoint
                        foreach (TaxiNode runwayEntryNode in takeoffSpot.EntryPoints)
                        {
                            findShortestPaths(_taxiNodes, runwayEntryNode, size);
                            foreach (Parking parking in _parkings)
                            {
                                or.AddResult(size, parking.NearestNode, parking, takeoffSpot);
                            }
                        }
                    }
                    or.WriteRoutes();
                }
            }
        }

        private void findShortestPaths(IEnumerable<TaxiNode> nodes, TaxiNode targetNode, int size)
        {
            List<TaxiNode> untouchedNodes = nodes.ToList();
            List<TaxiNode> touchedNodes = new List<TaxiNode>();

            foreach (TaxiNode node in nodes)
            {
                node.DistanceToTarget = double.MaxValue;
                node.NextNodeToTarget = null;
            }

            // Setup the targetnode
            targetNode.DistanceToTarget = 0;
            targetNode.NextNodeToTarget = null;

            foreach (TaxiEdge vto in targetNode.IncomingNodes)
            {
                if (size > vto.MaxSize)
                    continue;

                if (untouchedNodes.Contains(vto.StartNode) && !touchedNodes.Contains(vto.StartNode))
                {
                    untouchedNodes.Remove(vto.StartNode);
                    touchedNodes.Add(vto.StartNode);
                }

                vto.StartNode.DistanceToTarget = vto.DistanceKM;
                vto.StartNode.NextNodeToTarget = targetNode;
                vto.StartNode.NameToTarget = vto.LinkName;
                vto.StartNode.PathIsRunway = vto.IsRunway;
                vto.StartNode.BearingToTarget = VortexMath.BearingRadians(vto.StartNode, targetNode);
            }

            //doneNodes.Add(targetNode);
            untouchedNodes.Remove(targetNode);

            // and branch out from there
            while (touchedNodes.Count() > 0)
            {
                double min = touchedNodes.Min(a => a.DistanceToTarget);
                targetNode = touchedNodes.FirstOrDefault(a => a.DistanceToTarget == min);

                foreach (TaxiEdge incoming in targetNode.IncomingNodes)
                {
                    if (size > incoming.MaxSize)
                        continue;

                    // Try to force smaller aircraft to take their specific routes
                    double penalizedDistance = incoming.DistanceKM; // vto.RelativeDistance * (1.0 + 2.0 * (vto.MaxSize - size));

                    //double bearingToTarget = VortexMath.BearingRadians(incoming.SourceNode.Latitude, incoming.SourceNode.Longitude, targetNode.Latitude, targetNode.Longitude);
                    //double turnToTarget = VortexMath.AbsTurnAngle(targetNode.BearingToTarget, incoming.Bearing);

                        //penalizedDistance += 5.0;
                    //else if (turnToTarget > VortexMath.PI033)
                    //    penalizedDistance += 0.01;


                    if (incoming.IsRunway)
                        penalizedDistance += 1.0;

                    if (incoming.StartNode.DistanceToTarget > (targetNode.DistanceToTarget + penalizedDistance))
                    {
                        if (untouchedNodes.Contains(incoming.StartNode) && !touchedNodes.Contains(incoming.StartNode))
                        {
                            untouchedNodes.Remove(incoming.StartNode);
                            touchedNodes.Add(incoming.StartNode);
                        }

                        incoming.StartNode.DistanceToTarget = (targetNode.DistanceToTarget + penalizedDistance);
                        incoming.StartNode.NextNodeToTarget = targetNode;
                        incoming.StartNode.NameToTarget = incoming.LinkName;
                        incoming.StartNode.PathIsRunway = incoming.IsRunway;
                        incoming.StartNode.BearingToTarget = incoming.Bearing;
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

            TaxiNode startNode = _nodeDict[va];
            TaxiNode endNode = _nodeDict[vb];

            TaxiEdge outgoingEdge = _edges.SingleOrDefault(e => (e.StartNode.Id == va && e.EndNode.Id == vb));
            if (outgoingEdge != null)
                // todo: report warning
                outgoingEdge.MaxSize = Math.Max(outgoingEdge.MaxSize, maxSize);
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
                    incomingEdge.MaxSize = Math.Max(incomingEdge.MaxSize, maxSize);
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
            if (tokens[5] != "helos") // todo: helos
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
