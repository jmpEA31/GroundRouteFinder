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
            if (_usageNodes.ContainsKey(usage))
                return _usageNodes[usage];
            else
                return null;
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

            IEnumerable<TaxiNode> runwayNodes = taxiEdges.Where(te=>te.IsRunway).Select(te => te.StartNode).Concat(taxiEdges.Where(te => te.IsRunway).Select(te => te.EndNode)).Distinct();
            Console.WriteLine($"Runway nodes {runwayNodes.Count()}");
            foreach (TaxiNode node in runwayNodes)
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

            Console.WriteLine($"Near {NearestNode.Id} Displaced {DisplacedNode.Id}");

            // Find the nodes that make up this runway: first find an edge connected to the nearest node
            IEnumerable<TaxiEdge> selectedEdges = taxiEdges.Where(te => te.IsRunway && (te.EndNode.Id == NearestNode.Id || te.StartNode.Id == NearestNode.Id));
            if (selectedEdges.Count() == 0)
            {
                Console.WriteLine("No edges");
                return false;
            }

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
                foreach (TaxiEdge mn in node.IncomingNodes)
                {
                    if (mn.IsRunway)
                        continue;

                    double entryAngle = VortexMath.AbsTurnAngle(mn.Bearing, Bearing);
                    if (entryAngle <= 0.6 * VortexMath.PI) // allow a turn of roughly 100 degrees, todo: maybe lower this?
                    {
                        selectedOne = true;
                        takeOffSpot.EntryPoints.Add(mn.StartNode);
                    }
                }

                // If the node had a good entry, mark '1 node' as found
                if (selectedOne)
                {
                    selectedNodes++;
                    TakeOffSpots.Add(takeOffSpot);
                }
            }
            Console.WriteLine($"Selected Entries: {selectedNodes}");

        }

        private void findExits(IEnumerable<TaxiNode> runwayNodes, IEnumerable<TaxiNode> taxiNodes, IEnumerable<TaxiEdge> taxiEdges)
        {
            Tracking tracking = new Tracking();
            double reduced1Length = double.MaxValue;

            foreach (TaxiNode onRunwayNode in runwayNodes)
            {
                // Find nodes that have the current runway node in an incoming edge
                IEnumerable<TaxiEdge> exitEdges = taxiEdges.Where(edge => edge.StartNode.Id == onRunwayNode.Id);
                exitEdges = exitEdges.Where(ee => !runwayNodes.Select(n => n.Id).Contains(ee.EndNode.Id));

                foreach (TaxiEdge exit in exitEdges)
                {
                    TaxiNode offRunwayNode = taxiNodes.Single(tn => tn.Id == exit.EndNode.Id);
                    TaxiEdge mn = offRunwayNode.IncomingNodes.SingleOrDefault(inc => inc.StartNode.Id == onRunwayNode.Id);
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
                        if (Math.Abs(exitAngle) < VortexMath.Deg060Rad)
                        {
                            tracking.MaxLengthNode = onRunwayNode;
                            getOrCreate(RunwayNodeUsage.ExitMax).Roles[(int)(exitAngle < 0 ? UsageNodes.Role.Left : UsageNodes.Role.Right)] = new NodeUsage(onRunwayNode, offRunwayNode, exitDistance);
                        }
                    }
                    else if (tracking.ReducedNode1 == null || tracking.ReducedNode1 == onRunwayNode)
                    {
                        if (Math.Abs(exitAngle) < VortexMath.Deg0475Rad && exitDistance >= (Length * 0.4))
                        {
                            tracking.ReducedNode1 = onRunwayNode;
                            reduced1Length = exitDistance;
                            getOrCreate(RunwayNodeUsage.ExitReduced1).Roles[(int)(exitAngle < 0 ? UsageNodes.Role.Left : UsageNodes.Role.Right)] = new NodeUsage(onRunwayNode, offRunwayNode, exitDistance);
                        }
                    }
                    else if (tracking.ReducedNode2 == null || tracking.ReducedNode2 == onRunwayNode)
                    {
                        if (Math.Abs(exitAngle) < VortexMath.Deg0475Rad && exitDistance >= (Length * 0.4) && exitDistance < reduced1Length - 0.3)
                        {
                            tracking.ReducedNode2 = onRunwayNode;
                            getOrCreate(RunwayNodeUsage.ExitReduced2).Roles[(int)(exitAngle < 0 ? UsageNodes.Role.Left : UsageNodes.Role.Right)] = new NodeUsage(onRunwayNode, offRunwayNode, exitDistance);
                        }
                    }
                }
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
                TaxiEdge edgeToNext = taxiEdges.SingleOrDefault(e => e.IsRunway && (e.StartNode.Id == currentNode.Id && e.EndNode.Id != previousNodeId));
                if (edgeToNext == null)
                    break;

                // Keep the current Id as the previous Id
                previousNodeId = currentNode.Id;

                // And get the new current node
                currentNode = taxiNodes.Single(n => n.Id == edgeToNext.EndNode.Id);
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
