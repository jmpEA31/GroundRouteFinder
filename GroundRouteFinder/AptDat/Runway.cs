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

        public Dictionary<uint, RunwayExitNode> RunwayExits;

        private bool _availableForLanding = true;
        public bool AvailableForLanding
        {
            get { return _availableForLanding || AvailableForVFR;  }
            set { _availableForLanding = value;  }
        }

        private bool _availableForTakeOff = true;
        public bool AvailableForTakeOff
        {
            get { return _availableForTakeOff || AvailableForVFR; }
            set { _availableForTakeOff = value; }
        }

        public bool AvailableForVFR = true;

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
            Log($"Analyzing Runway {Designator}");

            RunwayExits = new Dictionary<uint, RunwayExitNode>();

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

            if (AvailableForTakeOff)
                findEntries();

            if (AvailableForLanding)
                findExits(RunwayNodes.Reverse<TaxiNode>(), taxiNodes, taxiEdges);

            return true;
        }

        private void findEntries()
        {
            int selectedNodes = 0;
            bool displacedNodeFound = false;
            double lastSelectedRemainingDistance = double.MaxValue;

            // Look for entries into taxinodes along the runway. We want to select all possible entries from
            // the first two nodes with at least 1 entry
            foreach (TaxiNode node in RunwayNodes)
            {
                if (selectedNodes > 1)
                    break;

                // If the take off spot has been displaced, do not select entries at the start of the runway
                // todo: start with these, but keep search and replace them if better ones are found.
                //if (!displacedNodeFound)
                //{
                //    if (node == DisplacedNode)
                //        displacedNodeFound = true;
                //    else if (VortexMath.DistanceKM(node.Latitude, node.Longitude, DisplacedLatitude, DisplacedLongitude) < 0.150)
                //        displacedNodeFound = true;
                //    else
                //        continue;
                //}

                RunwayTakeOffSpot takeOffSpot = new RunwayTakeOffSpot();
                takeOffSpot.TakeOffNode = node;
                double remainingLength = VortexMath.DistanceKM(OppositeLatitude, OppositeLongitude, takeOffSpot.TakeOffNode.Latitude, takeOffSpot.TakeOffNode.Longitude);

                // Now inspect the current node
                bool selectedOne = false;
                foreach (TaxiEdge edge in node.IncomingEdges)
                {
                    if (edge.IsRunway)
                        continue;

                    double entryAngle = VortexMath.AbsTurnAngle(edge.Bearing, Bearing);
                    if (remainingLength > VortexMath.Feet4000Km &&  entryAngle <= VortexMath.Deg120Rad) // allow a turn of roughly 100 degrees, todo: maybe lower this?
                    {
                        // Skip entries that are close to the last selected one
                        // Hmm, this can go 1 loop higher
                        if (lastSelectedRemainingDistance - remainingLength > 0.2)
                        {
                            selectedOne = true;
                            takeOffSpot.EntryPoints.Add(edge.StartNode);
                        }
                    }
                }

                // If the node had a good entry, mark '1 node' as found
                if (selectedOne)
                {
                    selectedNodes++;
                    TakeOffSpots.Add(takeOffSpot);
                    lastSelectedRemainingDistance = remainingLength;
                }
            }

            foreach (RunwayTakeOffSpot tos in TakeOffSpots)
            {
                Log($" Entry to <{tos.TakeOffNode.Id}> from <{string.Join(", ", tos.EntryPoints.Select(ep=>ep.Id)),-10}> length: {VortexMath.DistanceKM(OppositeLatitude, OppositeLongitude, tos.TakeOffNode.Latitude, tos.TakeOffNode.Longitude)/VortexMath.Foot2Km:0}ft");
            }
        }

        public class RunwayExitNode
        {
            public double ExitDistance;
            public RunwayExit LeftExit;
            public RunwayExit RightExit;

            public RunwayExitNode()
            {
                LeftExit = null;
                RightExit = null;
            }
        }

        public class RunwayExit
        {
            public double ExitDistance;
            public double ExitAngle;
            public double TurnAngle;
            public TaxiNode OnRunwayNode;
            public TaxiNode OffRunwayNode;

            public RunwayExit()
            {

            }

            public override string ToString()
            {
                return $"{ExitDistance:0.00}km {OnRunwayNode.Id} Turn {ExitAngle * VortexMath.Rad2Deg}";
            }

        }

        private class ExitLengthComparer : IComparer<RunwayExit>
        {
            public int Compare(RunwayExit x, RunwayExit y)
            {
                return x.ExitDistance.CompareTo(y.ExitDistance);
            }
        }

        private void findExits(IEnumerable<TaxiNode> runwayNodes, IEnumerable<TaxiNode> taxiNodes, IEnumerable<TaxiEdge> taxiEdges)
        {
            ExitLengthComparer exitLengthComparer = new ExitLengthComparer();
            List<RunwayExit> exitsLeft = new List<RunwayExit>();
            List<RunwayExit> exitsRight = new List<RunwayExit>();

            // start at long distance
            foreach (TaxiNode onRunwayNode in runwayNodes)
            {
                // Find nodes that have the current runway node in an incoming edge
                IEnumerable<TaxiEdge> exitEdges = taxiEdges.Where(edge => edge.StartNode.Id == onRunwayNode.Id);
                exitEdges = exitEdges.Where(ee => !runwayNodes.Select(n => n.Id).Contains(ee.EndNode.Id));

                foreach (TaxiEdge exit in exitEdges)
                {
                    TaxiNode offRunwayNode = exit.EndNode;
                    double leaveBearing;

                    // If there is only one link continuing from the current node use that as link's bearing
                    IEnumerable<TaxiEdge> secondEdges = taxiEdges.Where(inc => inc.StartNode.Id == offRunwayNode.Id && inc.EndNode.Id != onRunwayNode.Id);
                    if (secondEdges.Count() == 1)
                    {
                        leaveBearing = secondEdges.First().Bearing;
                    }
                    else
                    {
                        leaveBearing = exit.Bearing;
                    }

                    double exitAngle = VortexMath.TurnAngle(Bearing, leaveBearing); // sign indicates left or right turn
                    if (Math.Abs(exitAngle) > VortexMath.Deg100Rad)
                        break;

                    double exitDistance = VortexMath.DistanceKM(this, onRunwayNode);
                    if (exitDistance < VortexMath.Feet3000Km)
                        break;

                    if (exitAngle > 0)
                        exitsRight.Add(new RunwayExit() { ExitAngle = exitAngle, TurnAngle = Math.Abs(exitAngle), ExitDistance = exitDistance, OnRunwayNode = onRunwayNode, OffRunwayNode = offRunwayNode });
                    else
                        exitsLeft.Add(new RunwayExit() { ExitAngle = exitAngle, TurnAngle = Math.Abs(exitAngle), ExitDistance = exitDistance, OnRunwayNode = onRunwayNode, OffRunwayNode = offRunwayNode });
                }
            }

            analyzeExits(exitsLeft);
            analyzeExits(exitsRight);
        }

        private void analyzeExits(List<RunwayExit> exits)
        {
            if (exits.Count() > 0)
            {
                exits.Sort(new ExitLengthComparer());

                RunwayExit shortExit = exits.First();
                RunwayExit mediumExit = null;
                RunwayExit longExit = null;
                RunwayExit maxExit = null;

                foreach (RunwayExit exit in exits)
                {
                    if (exit.ExitDistance <= VortexMath.Feet5000Km && shortExit.TurnAngle > exit.TurnAngle)
                    {
                        shortExit = exit;
                    }
                    else if (exit != shortExit && exit.ExitDistance > VortexMath.Feet5000Km)
                    {
                        if (mediumExit == null)
                        {
                            mediumExit = exit;
                        }
                        else if (exit.ExitDistance < VortexMath.Feet6500Km && mediumExit.TurnAngle > exit.TurnAngle)
                        {
                            mediumExit = exit;
                        }
                        else if (exit.ExitDistance > VortexMath.Feet6500Km)
                        {
                            if (longExit == null)
                            {
                                longExit = exit;
                            }
                            else if (exit.ExitDistance < VortexMath.Feet8000Km && longExit.TurnAngle > exit.TurnAngle)
                            {
                                longExit = exit;
                            }
                            else if (exit.ExitDistance > VortexMath.Feet8000Km)
                            {
                                if (maxExit == null)
                                {
                                    maxExit = exit;
                                }
                                else if (maxExit.TurnAngle > exit.TurnAngle)
                                {
                                    maxExit = exit;
                                }
                            }
                        }
                    }
                }

                mergeExit(shortExit);
                mergeExit(mediumExit);
                mergeExit(longExit);
                mergeExit(maxExit);
            }
        }

        private void mergeExit(RunwayExit exit)
        {
            if (exit == null)
                return;

            if (!RunwayExits.ContainsKey(exit.OnRunwayNode.Id))
            {
                RunwayExits[exit.OnRunwayNode.Id] = new RunwayExitNode();
            }

            RunwayExits[exit.OnRunwayNode.Id].ExitDistance = exit.ExitDistance;
            if (exit.ExitAngle < 0)
                RunwayExits[exit.OnRunwayNode.Id].LeftExit = exit;
            else
                RunwayExits[exit.OnRunwayNode.Id].RightExit = exit;
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
