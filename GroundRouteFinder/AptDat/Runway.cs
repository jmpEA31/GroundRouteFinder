using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder.AptDat
{
    public class Runway : LocationObject
    {
        private static char[] _invalidChars = Path.GetInvalidFileNameChars();

        private string _designator;
        private string _designatorSafe;
        public string Designator
        {
            get { return _designator; }

            set
            {
                _designator = value;
                _designatorSafe = new string(value.Select(c => _invalidChars.Contains(c) ? '_' : c).ToArray());
            }
        }

        public string DesignatorFileName { get { return _designatorSafe; } }

        public double Displacement;

        public TaxiNode NearestNode;

        public double DisplacedLatitude;
        public double DisplacedLongitude;
        public TaxiNode DisplacedNode;

        public double OppositeLatitude;
        public double OppositeLongitude;

        public double Length;

        public List<RunwayTakeOffSpot> TakeOffSpots;
        public List<TaxiNode> RunwayNodes;

        public double Bearing;

        public class NodeUsage
        {
            public TaxiNode OnRunwayNode;
            public TaxiNode OffRunwayNode;
            public double EffectiveLength;
            public List<TaxiNode> FixedPath;

            public NodeUsage(TaxiNode onRunwayNode, TaxiNode offRunwayNode, double exitDistance)
            {
                OnRunwayNode = onRunwayNode;
                OffRunwayNode = offRunwayNode;
                EffectiveLength = exitDistance;
                FixedPath = new List<TaxiNode>();
            }
        }

        public class UsageNodes
        {
            public enum Role : int
            {
                Left = 0,
                Right = 1,
                Max
            }

            public NodeUsage[] Roles;

            public UsageNodes()
            {
                Roles = new NodeUsage[(int)Role.Max];
            }
        }

        public class Tracking
        {
            public TaxiNode MaxLengthNode;
            public TaxiNode ReducedNode1;
            public TaxiNode ReducedNode2;
            public TaxiNode MinimumNode;

            public Tracking()
            {
                MaxLengthNode = null;
                ReducedNode1 = null;
                ReducedNode2 = null;
                MinimumNode = null;
            }
        }

        public enum RunwayNodeUsage : int
        {
            EntryNormal = 0,
            EntryDisplaced,
            ExitMax,
            ExitReduced1,
            ExitReduced2,
            ExitShort,
            Max
        }

        public Dictionary<RunwayNodeUsage, UsageNodes> _usageNodes;

        public UsageNodes GetNodesForUsage(RunwayNodeUsage usage)
        {
            return _usageNodes[usage];
        }

        public Runway(string designator, double latitude, double longitude, double displacement, double oppositeLatitude, double oppositeLongitude)
            : base(latitude, longitude)
        {
            NearestNode = null;
            DisplacedNode = null;
            TakeOffSpots = new List<RunwayTakeOffSpot>();

            Designator = designator;
            Latitude = latitude;
            Longitude = longitude;
            Displacement = displacement;
            OppositeLatitude = oppositeLatitude;
            OppositeLongitude = oppositeLongitude;

            Bearing = VortexMath.BearingRadians(Latitude, Longitude, OppositeLatitude, OppositeLongitude);
            VortexMath.PointFrom(Latitude, Longitude, Bearing, Displacement, ref DisplacedLatitude, ref DisplacedLongitude);
            Length = VortexMath.DistanceKM(DisplacedLatitude, DisplacedLongitude, OppositeLatitude, OppositeLongitude);
        }

        public bool Analyze(IEnumerable<TaxiNode> taxiNodes, IEnumerable<TaxiEdge> taxiEdges)
        {
            _usageNodes = new Dictionary<RunwayNodeUsage, UsageNodes>();

            // Find the taxi nodes closest to the start of the runway and the displaced start
            double shortestDistance = double.MaxValue;
            double shortestDisplacedDistance = double.MaxValue;

            foreach (TaxiNode node in taxiNodes.Where(v => v.IsRunwayNode))
            {
                double d = VortexMath.DistancePyth(node.Latitude, node.Longitude, DisplacedLatitude, DisplacedLongitude);
                if (d < shortestDisplacedDistance)
                {
                    shortestDisplacedDistance = d;
                    DisplacedNode = node;
                }

                d = VortexMath.DistancePyth(this, node);
                if (d < shortestDistance)
                {
                    shortestDistance = d;
                    NearestNode = node;
                }
            }

            // Find the nodes that make up this runway: first find an edge connected to the nearest node
            IEnumerable<TaxiEdge> selectedEdges = taxiEdges.Where(te => te.IsRunway && (te.EndNodeId == NearestNode.Id || te.StartNodeId == NearestNode.Id));
            if (selectedEdges.Count() == 0)
                return false;

            // The name of the link gives the name of the runway, use it to retrieve all edges for this runway
            string edgeKey = selectedEdges.First().LinkName;
            selectedEdges = taxiEdges.Where(te => te.LinkName == edgeKey);

            RunwayNodes = findNodeChain(taxiNodes, selectedEdges);

            findEntries();
            findExits(RunwayNodes.Reverse<TaxiNode>(), taxiNodes, taxiEdges);
            dumpExits(taxiNodes, taxiEdges);

            return true;
        }

        private void findEntries()
        {
            int selectedNodes = 0;
            bool displacedNodeFound = false;

            // Look for entries into taxinodes along the runway. We want to select all possible entries from
            // the first two nodes with at least 1 entry
            foreach (TaxiNode node in RunwayNodes)
            {
                if (selectedNodes > 1)
                    break;

                // If the take off spot has been displaced, do not select entries at the start of the runway
                // todo: start with these, but keep search and replace them if better ones are found.
                if (!displacedNodeFound)
                {
                    if (node == DisplacedNode)
                        displacedNodeFound = true;
                    else if (VortexMath.DistanceKM(node.Latitude, node.Longitude, DisplacedLatitude, DisplacedLongitude) < 0.150)
                        displacedNodeFound = true;
                    else
                        continue;
                }

                RunwayTakeOffSpot takeOffSpot = new RunwayTakeOffSpot();
                takeOffSpot.TakeOffNode = node;

                displacedNodeFound = true;

                // Now inspect the current node
                bool selectedOne = false;
                foreach (MeasuredNode mn in node.IncomingNodes)
                {
                    if (mn.IsRunway)
                        continue;

                    double entryAngle = VortexMath.AbsTurnAngle(mn.Bearing, Bearing);
                    if (entryAngle <= 0.6 * VortexMath.PI) // allow a turn of roughly 100 degrees, todo: maybe lower this?
                    {
                        selectedOne = true;
                        takeOffSpot.EntryPoints.Add(mn.SourceNode);
                    }
                }

                // If the node had a good entry, mark '1 node' as found
                if (selectedOne)
                {
                    selectedNodes++;
                    TakeOffSpots.Add(takeOffSpot);
                }
            }
        }

        private void findExits(IEnumerable<TaxiNode> runwayNodes, IEnumerable<TaxiNode> taxiNodes, IEnumerable<TaxiEdge> taxiEdges)
        {
            Tracking tracking = new Tracking();
            double reduced1Length = double.MaxValue;

            foreach (TaxiNode onRunwayNode in RunwayNodes)
            {
                // Find nodes that have the current runway node in an incoming edge
                IEnumerable<TaxiEdge> exitEdges = taxiEdges.Where(edge => edge.StartNodeId == onRunwayNode.Id);
                exitEdges = exitEdges.Where(ee => !runwayNodes.Select(n => n.Id).Contains(ee.EndNodeId));

                foreach (TaxiEdge exit in exitEdges)
                {
                    TaxiNode offRunwayNode = taxiNodes.Single(tn => tn.Id == exit.EndNodeId);
                    MeasuredNode mn = offRunwayNode.IncomingNodes.SingleOrDefault(inc => inc.SourceNode.Id == onRunwayNode.Id);
                    if (mn == null)
                        continue;

                    double exitAngle = VortexMath.TurnAngle(Bearing, mn.Bearing); // sign indicates left or right turn
                    double exitDistance = VortexMath.DistanceKM(this, onRunwayNode);

                    if (Math.Abs(exitAngle) < VortexMath.Deg100Rad && exitDistance >= (Length * 0.4))
                    {
                        // If we found a shorter candidate clear all results for the Short Exit:
                        if (tracking.MinimumNode != null && tracking.MinimumNode != onRunwayNode)
                        {
                            tracking.MinimumNode = null;
                            getOrCreate(RunwayNodeUsage.ExitShort).Roles[(int)UsageNodes.Role.Left] = null;
                            getOrCreate(RunwayNodeUsage.ExitShort).Roles[(int)UsageNodes.Role.Right] = null;
                        }

                        // If this a new node, or the same as already present, add it
                        if (tracking.MinimumNode == null || tracking.MinimumNode == onRunwayNode)
                        {
                            tracking.MinimumNode = onRunwayNode;
                            getOrCreate(RunwayNodeUsage.ExitShort).Roles[(int)(exitAngle < 0 ? UsageNodes.Role.Left : UsageNodes.Role.Right)] = new NodeUsage(onRunwayNode, offRunwayNode, exitDistance);
                        }
                    }

                    if (tracking.MaxLengthNode == null || tracking.MaxLengthNode == onRunwayNode)
                    {
                        if (Math.Abs(exitAngle) < VortexMath.Deg100Rad)
                        {
                            tracking.MaxLengthNode = onRunwayNode;
                            getOrCreate(RunwayNodeUsage.ExitMax).Roles[(int)(exitAngle < 0 ? UsageNodes.Role.Left : UsageNodes.Role.Right)] = new NodeUsage(onRunwayNode, offRunwayNode, exitDistance);
                        }
                    }
                    else if (tracking.ReducedNode1 == null || tracking.ReducedNode1 == onRunwayNode)
                    {
                        if (Math.Abs(exitAngle) < VortexMath.Deg475Rad && exitDistance >= (Length * 0.4))
                        {
                            tracking.ReducedNode1 = onRunwayNode;
                            reduced1Length = exitDistance;
                            getOrCreate(RunwayNodeUsage.ExitReduced1).Roles[(int)(exitAngle < 0 ? UsageNodes.Role.Left : UsageNodes.Role.Right)] = new NodeUsage(onRunwayNode, offRunwayNode, exitDistance);
                        }
                    }
                    else if (tracking.ReducedNode2 == null || tracking.ReducedNode2 == onRunwayNode)
                    {
                        if (Math.Abs(exitAngle) < VortexMath.Deg475Rad && exitDistance >= (Length * 0.4) && exitDistance < reduced1Length - 0.3)
                        {
                            tracking.ReducedNode2 = onRunwayNode;
                            getOrCreate(RunwayNodeUsage.ExitReduced2).Roles[(int)(exitAngle < 0 ? UsageNodes.Role.Left : UsageNodes.Role.Right)] = new NodeUsage(onRunwayNode, offRunwayNode, exitDistance);
                        }
                    }
                }
            }

            foreach (UsageNodes usage in _usageNodes.Values)
            {
                // todo: find a fixed exit route out of the runway...
            }
        }

        private UsageNodes getOrCreate(RunwayNodeUsage usage)
        {
            if (!_usageNodes.ContainsKey(usage))
                _usageNodes[usage] = new UsageNodes();

            return _usageNodes[usage];
        }


        private void dumpExits(IEnumerable<TaxiNode> taxiNodes, IEnumerable<TaxiEdge> taxiEdges)
        {
            Console.WriteLine($"Runway: {Designator}");
            foreach (KeyValuePair<RunwayNodeUsage, UsageNodes> node in _usageNodes)
            {
                Console.WriteLine($" {node.Key}");
                for (int i = 0; i < (int)UsageNodes.Role.Max; i++)
                {
                    NodeUsage usage = node.Value.Roles[i];

                    if (usage != null)
                    {
                        Console.WriteLine($"  {(UsageNodes.Role)i,5} {usage.OnRunwayNode.Id}->{usage.OffRunwayNode.Id} @{usage.EffectiveLength:0.0}km");
/*
 * The following attempts to find a gentle path off the runway. Intention is to find the shortest route from
 * the end of the gentel path. This is to prevent routes with >90 degree turns for crossing high speed exits for example
 * 
                        TaxiNode onRwy = taxiNodes.Single(tn => tn.Id == usage.OnRunwayNode.Id);
                        TaxiNode offRwy = taxiNodes.Single(tn => tn.Id == usage.OffRunwayNode.Id);

                        double currentBearing = VortexMath.BearingRadians(onRwy.Latitude, onRwy.Longitude, offRwy.Latitude, offRwy.Longitude);

                        TaxiEdge te = taxiEdges.SingleOrDefault(tex => tex.StartNodeId == usage.OnRunwayNode.Id && tex.EndNodeId == usage.OffRunwayNode.Id);

                        TaxiNode fromNode = usage.OffRunwayNode;
                        int rep = 0;
                        while (fromNode != null && rep < 5)
                        {
                            rep++;

                            IEnumerable<TaxiEdge> nxs = taxiEdges.Where(tex => tex.StartNodeId == fromNode.Id);
                            //Console.WriteLine($" {nxs.Count()} edges go from off runway point {fromNode.Id}: {string.Join(",", nxs.Select(n => n.EndNodeId))}");

                            double turnAngleMin = double.MaxValue;
                            TaxiNode bestNode = null;
                            string activeFor = "";
                            foreach (TaxiEdge tx in nxs)
                            {
                                TaxiNode nextNode = taxiNodes.Single(tn => tn.Id == tx.EndNodeId);
                                double nextBearing = VortexMath.BearingRadians(offRwy.Latitude, offRwy.Longitude, nextNode.Latitude, nextNode.Longitude);

                                double turnAngleNow = VortexMath.AbsTurnAngle(currentBearing, nextBearing);
                                if (turnAngleMin > turnAngleNow)
                                {
                                    turnAngleMin = turnAngleNow;
                                    bestNode = nextNode;
                                    activeFor = tx.ActiveFor;
                                }
                            }

                            if (bestNode != null)
                            {
                                usage.FixedPath.Add(bestNode);
                                //Console.WriteLine($"Best: {bestNode.Id} {activeFor} {turnAngleMin}");
                                if (turnAngleMin > VortexMath.PI05 || VortexMath.DistanceKM(usage.OnRunwayNode.Latitude, usage.OnRunwayNode.Longitude, bestNode.Latitude, bestNode.Longitude) > 0.200)
                                {
                                    bestNode = null;
                                }
                            }
                            fromNode = bestNode;
                        }
*/
                    }
                    else
                    {
                        Console.WriteLine($"  {(UsageNodes.Role)i,5} -x-x-x-");
                    }
                }
            }
        }

        /// <summary>
        /// Find the chain of TaxiNodes that represent this runway
        /// </summary>
        /// <param name="taxiNodes"></param>
        /// <param name="taxiEdges"></param>
        /// <returns></returns>
        private List<TaxiNode> findNodeChain(IEnumerable<TaxiNode> taxiNodes, IEnumerable<TaxiEdge> taxiEdges)
        {
            List<TaxiNode> nodes = new List<TaxiNode>();

            // Start with the node nearest to the runway lat/lon
            TaxiNode currentNode = NearestNode;
            nodes.Add(currentNode);
            ulong previousNodeId = 0;

            do
            {
                // Now find an edge that is marked as 'runway' and that starts at the current node, bt does not lead to the previous node
                // todo: test with crossing runways
                TaxiEdge edgeToNext = taxiEdges.SingleOrDefault(e => e.IsRunway && (e.StartNodeId == currentNode.Id && e.EndNodeId != previousNodeId));
                if (edgeToNext == null)
                    break;

                // Keep the current Id as the previous Id
                previousNodeId = currentNode.Id;

                // And get the new current node
                currentNode = taxiNodes.Single(n => n.Id == edgeToNext.EndNodeId);
                if (currentNode != null)
                    nodes.Add(currentNode);
            }
            while (currentNode != null);

            return nodes;
        }


        public override string ToString()
        {
            return Designator;
        }
    }
}
