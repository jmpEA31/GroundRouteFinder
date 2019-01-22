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
        }

        public class UsageNodes
        {
            public NodeUsage Left;
            public NodeUsage Right;

            public UsageNodes()
            {
                Left = null;
                Right = null;
            }
        }

        public enum RunwayNodeUsage
        {
            EntryNormal,
            EntryDisplaced,
            ExitMax,
            ExitReduced1,
            ExitReduced2,
            ExitShort
        }

        public Dictionary<RunwayNodeUsage, UsageNodes> _usageNodes;

        public IEnumerable<TaxiNode> GetNodesForUsage(RunwayNodeUsage usage)
        {
            return null;
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

            List<TaxiNode> runwayChain = findChain(taxiNodes, selectedEdges);

            int selectedNodes = 0;
            bool displacedNodeFound = false;

            // Look for entries into taxinodes along the runway. We want to select all possible entries from
            // the first two nodes with at least 1 entry
            foreach (TaxiNode node in runwayChain)
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
            runwayChain.Reverse();
            selectedNodes = 0;

            List<ExitData> leftExits = new List<ExitData>();
            List<ExitData> rightExits = new List<ExitData>();

            bool leftMaxFound = false;
            bool leftReduced1Found = false;
            bool leftReduced2Found = false;
            bool rightMaxFound = false;

            foreach (TaxiNode node in runwayChain)
            {
                // Find nodes that have the current runway node in an incoming edge
                IEnumerable<TaxiEdge> exitEdges = taxiEdges.Where(edge => edge.StartNodeId == node.Id);
                exitEdges = exitEdges.Where(ee => !runwayChain.Select(n => n.Id).Contains(ee.EndNodeId));

                if (exitEdges.Count() == 0)
                {
                    //Console.WriteLine($" No links exiting out of {node.Id} @{VortexMath.DistanceKM(r.Latitude, r.Longitude, node.Latitude, node.Longitude):0.00}km");
                }

                foreach (TaxiEdge exit in exitEdges)
                {
                    TaxiNode exitNode = taxiNodes.Single(tn => tn.Id == exit.EndNodeId);
                    MeasuredNode mn = exitNode.IncomingNodes.SingleOrDefault(inc => inc.SourceNode.Id == node.Id);
                    if (mn == null)
                        continue;

                    double exitAngle = VortexMath.TurnAngle(Bearing, mn.Bearing); // sign indicates left or right turn
                    double exitDistance = VortexMath.DistanceKM(Latitude, Longitude, node.Latitude, node.Longitude);

                    if (exitAngle < 0)
                    {
                        if (Math.Abs(exitAngle) < (100.0 * VortexMath.Deg2Rad) && exitDistance >= (Length * 0.4))
                        {
                            // store mins
                        }

                        if (!leftMaxFound && Math.Abs(exitAngle) < 100.0 * VortexMath.Deg2Rad)
                        {
                            if (!_usageNodes.ContainsKey(RunwayNodeUsage.ExitMax))
                                _usageNodes[RunwayNodeUsage.ExitMax] = new UsageNodes();

                            _usageNodes[RunwayNodeUsage.ExitMax].Left = new NodeUsage() { OnRunwayNode = node, OffRunwayNode = exitNode, EffectiveLength = exitDistance };
                            leftMaxFound = true;
                        }

                        if (leftMaxFound && !leftReduced1Found && Math.Abs(exitAngle) < 46.0 * VortexMath.Deg2Rad && exitDistance >= (Length * 0.4))
                        {
                            if (!_usageNodes.ContainsKey(RunwayNodeUsage.ExitReduced1))
                                _usageNodes[RunwayNodeUsage.ExitReduced1] = new UsageNodes();

                            _usageNodes[RunwayNodeUsage.ExitReduced1].Left = new NodeUsage() { OnRunwayNode = node, OffRunwayNode = exitNode, EffectiveLength = exitDistance };
                            leftReduced1Found = true;
                        }

                        if (leftMaxFound && leftReduced1Found && !leftReduced2Found)
                        {                            
                            if (Math.Abs(exitAngle) < 46.0 * VortexMath.Deg2Rad && exitDistance >= (Length * 0.4) && exitDistance < _usageNodes[RunwayNodeUsage.ExitReduced1].Left.EffectiveLength - 0.3)
                            {
                                if (!_usageNodes.ContainsKey(RunwayNodeUsage.ExitReduced2))
                                    _usageNodes[RunwayNodeUsage.ExitReduced2] = new UsageNodes();

                                _usageNodes[RunwayNodeUsage.ExitReduced2].Left = new NodeUsage() { OnRunwayNode = node, OffRunwayNode = exitNode, EffectiveLength = exitDistance };
                                leftReduced2Found = true;
                            }
                        }

                    }


                    if (Math.Abs(exitAngle) < 1.1 * VortexMath.PI05)
                    {
                        if (exitAngle < 0)
                        {
                            leftExits.Add(new ExitData() { RunwayNode = node, ExitNode = exitNode, ExitAngle = exitAngle, Distance = VortexMath.DistanceKM(Latitude, Longitude, node.Latitude, node.Longitude) });
                        }
                        else
                        {
                            rightExits.Add(new ExitData() { RunwayNode = node, ExitNode = exitNode, ExitAngle = exitAngle, Distance = VortexMath.DistanceKM(Latitude, Longitude, node.Latitude, node.Longitude) });
                        }
                        //Console.WriteLine($"{r.Number} Leaving from {node.Id} to {exitNode.Id} at"+
                        //    $" {exitAngle * VortexMath.Rad2Deg:0.0} degrees, @{VortexMath.DistanceKM(r.Latitude, r.Longitude, node.Latitude, node.Longitude):0.00}km");
                    }
                }
            }

            dumpExits(leftExits);
            dumpExits(rightExits);

            return true;
        }


        private void dumpExits(List<ExitData> exits)
        {
            int selected = 0;
            ExitData selected1 = null;
            ExitData selected2 = null;
            ExitData selected3 = null;
            ExitData selected4 = null;
            foreach (ExitData ed in exits)
            {
                if (ed.ExitAngle < (100.0 * VortexMath.Deg2Rad) && ed.Distance >= (Length * 0.4))
                {
                    selected4 = ed;
                }

                switch (selected)
                {
                    case 0:
                        if (ed.ExitAngle < (100.0 * VortexMath.Deg2Rad) && ed.Distance >= (Length * 0.6))
                        {
                            selected1 = ed;
                            selected++;
                        }
                        break;
                    case 1:
                        if (ed.ExitAngle < (46.0 * VortexMath.Deg2Rad) && ed.Distance >= (Length * 0.4))
                        {
                            selected2 = ed;
                            selected++;
                        }
                        break;
                    case 2:
                        if (ed.ExitAngle < (46.0 * VortexMath.Deg2Rad) && ed.Distance >= (Length * 0.4) && (selected2.Distance - ed.Distance) > 0.3)
                        {
                            selected3 = ed;
                            selected++;
                        }

                        break;
                    default:
                        break;
                }
            }
            dumpExit(selected1, "max range");
            dumpExit(selected2, "first alternate");
            dumpExit(selected3, "second alternate");
            dumpExit(selected4, "minimum");
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
                TaxiEdge edgeToNext = taxiEdges.SingleOrDefault(e => e.IsRunway &&
                                                        ((e.StartNodeId == current.Id && e.EndNodeId != previousId) ||
                                                         (e.EndNodeId == current.Id && e.StartNodeId != previousId)));
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
