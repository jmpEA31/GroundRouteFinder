using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder.AptDat
{
    public class Runway
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

        public double Latitude;
        public double Longitude;
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

        private class ExitData
        {
            public TaxiNode RunwayNode;
            public TaxiNode ExitNode;
            public double Distance;
            public double ExitAngle;
        }

        public class NodeUsage
        {
            public TaxiNode OnRunwayNode;
            public TaxiNode OffRunwayNode;
            public double EffectiveLength;

            public NodeUsage(TaxiNode onRunwayNode, TaxiNode offRunwayNode, double exitDistance)
            {
                OnRunwayNode = onRunwayNode;
                OffRunwayNode = offRunwayNode;
                EffectiveLength = exitDistance;
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
            public bool MaxLengthFound;
            public TaxiNode MaxLengthNode;
            public bool Reduced1Found;
            public TaxiNode ReducedNode1;
            public bool Reduced2Found;
            public TaxiNode ReducedNode2;
            public bool MinimumFound;
            public TaxiNode MinimumNode;

            public Tracking()
            {
                MaxLengthFound = false;
                MaxLengthNode = null;
                Reduced1Found = false;
                ReducedNode1 = null;
                Reduced2Found = false;
                ReducedNode2 = null;
                MinimumFound = false;
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

            foreach (TaxiNode vx in taxiNodes.Where(v => v.IsRunwayNode))
            {
                double d = VortexMath.DistancePyth(vx.Latitude, vx.Longitude, DisplacedLatitude, DisplacedLongitude);
                if (d < shortestDisplacedDistance)
                {
                    shortestDisplacedDistance = d;
                    DisplacedNode = vx;
                }

                d = VortexMath.DistancePyth(vx.Latitude, vx.Longitude, Latitude, Longitude);
                if (d < shortestDistance)
                {
                    shortestDistance = d;
                    NearestNode = vx;
                }
            }

            // Find the nodes that make up this runway
            IEnumerable<TaxiEdge> selectedEdges = taxiEdges.Where(te => te.IsRunway && (te.EndNodeId == NearestNode.Id || te.StartNodeId == NearestNode.Id));
            if (selectedEdges.Count() == 0)
                return false;

            string edgeKey = selectedEdges.First().LinkName;
            selectedEdges = taxiEdges.Where(te => te.LinkName == edgeKey);

            RunwayNodes = findChain(taxiNodes, selectedEdges);

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

            // Do it again for exits
            RunwayNodes.Reverse();

            Tracking rightTracking = new Tracking();
            Tracking leftTracking = new Tracking();
            Tracking tracking = new Tracking();
            double reduced1Length = double.MaxValue;

            foreach (TaxiNode onRunwayNode in RunwayNodes)
            {
                // Find nodes that have the current runway node in an incoming edge
                IEnumerable<TaxiEdge> exitEdges = taxiEdges.Where(edge => edge.StartNodeId == onRunwayNode.Id);
                exitEdges = exitEdges.Where(ee => !RunwayNodes.Select(n => n.Id).Contains(ee.EndNodeId));

                foreach (TaxiEdge exit in exitEdges)
                {
                    TaxiNode offRunwayNode = taxiNodes.Single(tn => tn.Id == exit.EndNodeId);
                    MeasuredNode mn = offRunwayNode.IncomingNodes.SingleOrDefault(inc => inc.SourceNode.Id == onRunwayNode.Id);
                    if (mn == null)
                        continue;

                    double exitAngle = VortexMath.TurnAngle(Bearing, mn.Bearing); // sign indicates left or right turn
                    double exitDistance = VortexMath.DistanceKM(Latitude, Longitude, onRunwayNode.Latitude, onRunwayNode.Longitude);

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
            dumpExits();

            RunwayNodes.Reverse();
            return true;
        }


        private void evaluateExit(UsageNodes.Role role, TaxiNode onRunwayNode, TaxiNode offRunwayNode, double exitAngle, double exitDistance, Tracking tracking)
        {
            // Find the 'shortest' exit with a turn of less than 100 degrees and at least at 40% of the runway length
            if (Math.Abs(exitAngle) < VortexMath.Deg100Rad && exitDistance >= (Length * 0.4))
            {
                getOrCreate(RunwayNodeUsage.ExitShort).Roles[(int)role] = new NodeUsage(onRunwayNode, offRunwayNode, exitDistance);
                tracking.MinimumFound = true;
            }

            // Find the last exit with a turn less than 100 degrees
            if (!tracking.MaxLengthFound && exitAngle < VortexMath.Deg100Rad)
            {
                getOrCreate(RunwayNodeUsage.ExitMax).Roles[(int)role] = new NodeUsage(onRunwayNode, offRunwayNode, exitDistance);
                tracking.MaxLengthFound = true;
            }

            // Find an earlier exit between the max and 40% runway length, with an exit angle < 47.5
            else if (tracking.MaxLengthFound && !tracking.Reduced1Found && exitAngle < VortexMath.Deg475Rad && exitDistance >= (Length * 0.4))
            {
                getOrCreate(RunwayNodeUsage.ExitReduced1).Roles[(int)role] = new NodeUsage(onRunwayNode, offRunwayNode, exitDistance);
                tracking.Reduced1Found = true;
            }

            // Find an even earlier exit, at least 300m away from the previous but before 40% runway length with an exit angle < 47.5
            else if (tracking.Reduced1Found && !tracking.Reduced2Found)
            {
                if (Math.Abs(exitAngle) < VortexMath.Deg475Rad && exitDistance >= (Length * 0.4) && exitDistance < getOrCreate(RunwayNodeUsage.ExitReduced1).Roles[(int)role].EffectiveLength - 0.3)
                {
                    getOrCreate(RunwayNodeUsage.ExitReduced2).Roles[(int)role] = new NodeUsage(onRunwayNode, offRunwayNode, exitDistance);
                    tracking.Reduced2Found = true;
                }
            }
        }

        private UsageNodes getOrCreate(RunwayNodeUsage usage)
        {
            if (!_usageNodes.ContainsKey(usage))
                _usageNodes[usage] = new UsageNodes();

            return _usageNodes[usage];
        }


        private void dumpExits()
        {
            Console.WriteLine($"Runway: {Designator}");
            foreach (KeyValuePair<RunwayNodeUsage, UsageNodes> node in _usageNodes)
            {
                Console.WriteLine($" {node.Key}");
                for (int i=0;i<(int)UsageNodes.Role.Max;i++)
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



        private void dumpExit(ExitData exit, string note)
        {
            if (exit != null)
            {
                Console.WriteLine($"{Designator} Leaving from {exit.RunwayNode.Id} to {exit.ExitNode.Id} at {exit.ExitAngle * VortexMath.Rad2Deg:0.0} degrees, @{exit.Distance:0.00}km {note}");
            }
            else
            {
                Console.WriteLine($"{Designator}: None for {note}");
            }
        }

        private List<TaxiNode> findChain(IEnumerable<TaxiNode> runwayNodes, IEnumerable<TaxiEdge> taxiEdges)
        {
            List<TaxiNode> nodes = new List<TaxiNode>();
            TaxiNode current = NearestNode;
            nodes.Add(NearestNode);

            ulong previousId = 0;
            do
            {
                // todo: test with crossing runways
                TaxiEdge edgeToNext = taxiEdges.SingleOrDefault(e => e.IsRunway && (e.StartNodeId == current.Id && e.EndNodeId != previousId));
                if (edgeToNext == null)
                    break;

                previousId = current.Id;

                ulong nextId = (edgeToNext.StartNodeId == current.Id) ? edgeToNext.EndNodeId : edgeToNext.StartNodeId;
                current = runwayNodes.Single(n => n.Id == nextId);
                if (current != null)
                {
                    nodes.Add(current);
                }
            }
            while (current != null);

            return nodes;
        }


        public override string ToString()
        {
            return Designator;
        }
    }
}
