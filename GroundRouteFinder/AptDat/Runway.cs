using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder.AptDat
{
    public class EntryPoint
    {
        public TaxiNode OnRunwayNode;
        public TaxiNode OffRunwayNode;
        public double TakeoffLengthRemaining;
        public double TurnAngle;
    }

    public class ExitPoint
    {
        public TaxiNode OnRunwayNode;
        public TaxiNode OffRunwayNode;
        public double LandingLengthUsed;
        public double TurnAngle;
    }

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

        private Runway _oppositeEnd;
        public Runway OppositeEnd
        {
            get { return _oppositeEnd; }
            set
            {
                _oppositeEnd = value;
                if (_oppositeEnd != null)
                {
                    Bearing = VortexMath.BearingRadians(this, _oppositeEnd);
                    Length = VortexMath.DistanceKM(DisplacedLatitude, DisplacedLongitude, _oppositeEnd.Latitude, _oppositeEnd.Longitude);
                }
            }
        }

        public double Length;

        //        public List<RunwayTakeOffSpot> TakeOffSpots;
        public List<TaxiNode> RunwayNodes;
        public Dictionary<TaxiNode, List<EntryPoint>> EntryGroups;
        public Dictionary<TaxiNode, List<ExitPoint>> ExitGroups;

        public double Bearing;

//        public Dictionary<uint, RunwayExitNode> RunwayExits;

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

        public Runway(string designator, double latitude, double longitude, double displacement)
            : base(latitude, longitude)
        {
            NearestNode = null;
            DisplacedNode = null;
            EntryGroups = new Dictionary<TaxiNode, List<EntryPoint>>();
            ExitGroups = new Dictionary<TaxiNode, List<ExitPoint>>();

            //            TakeOffSpots = new List<RunwayTakeOffSpot>();

            Designator = designator;
            Latitude = latitude;
            Longitude = longitude;
            Displacement = displacement;
            VortexMath.PointFrom(Latitude, Longitude, Bearing, Displacement, ref DisplacedLatitude, ref DisplacedLongitude);

            OppositeEnd = null;
        }

        public bool Analyze(IEnumerable<TaxiNode> taxiNodes, IEnumerable<TaxiEdge> taxiEdges)
        {
            //Log($"Analyzing Runway {Designator}");

            //RunwayExits = new Dictionary<uint, RunwayExitNode>();

            // Find the taxi nodes closest to the start of the runway and the displaced start
            double shortestDistance = double.MaxValue;
            double shortestDisplacedDistance = double.MaxValue;

            IEnumerable<TaxiNode> runwayNodes = taxiEdges.Where(te=>te.IsRunway).Select(te => te.StartNode).Concat(taxiEdges.Where(te => te.IsRunway).Select(te => te.EndNode)).Distinct();
            //Console.WriteLine($"Runway nodes {runwayNodes.Count()}");
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

            //Console.WriteLine($"Near {NearestNode.Id} Displaced {DisplacedNode.Id}");

            // Find the nodes that make up this runway: first find an edge connected to the nearest node
            IEnumerable<TaxiEdge> selectedEdges = taxiEdges.Where(te => te.IsRunway && (te.EndNode.Id == NearestNode.Id || te.StartNode.Id == NearestNode.Id));
            if (selectedEdges.Count() == 0)
            {
                //Console.WriteLine("No edges");
                return false;
            }

            // The name of the link gives the name of the runway, use it to retrieve all edges for this runway
            string edgeKey = selectedEdges.First().LinkName;
            selectedEdges = taxiEdges.Where(te => te.LinkName == edgeKey);

            RunwayNodes = findNodeChain(taxiNodes, selectedEdges);

            if (AvailableForTakeOff)
                findEntries();

            if (AvailableForLanding)
                findExits2();
//                findExits(RunwayNodes.Reverse<TaxiNode>(), taxiNodes, taxiEdges);

            return true;
        }

        private void findEntries()
        {
            EntryGroups.Clear();

            TaxiNode groupStartNode = null;
            bool foundGroups = false;

            // Nodes are ordered, longest remaining runway to runway end
            foreach (TaxiNode node in RunwayNodes)
            {
                double takeoffLengthRemaining = VortexMath.DistanceKM(node, OppositeEnd);
                if (takeoffLengthRemaining < VortexMath.Feet4000Km)
                    break;

                foreach (TaxiEdge edge in node.IncomingEdges)
                {
                    if (edge.IsRunway)
                        continue;

                    double entryAngle = VortexMath.TurnAngle(edge.Bearing, Bearing);
                    if (Math.Abs(entryAngle) > VortexMath.Deg120Rad)
                        continue;

                    if (groupStartNode == null)
                    {
                        groupStartNode = node;
                        EntryGroups.Add(node, new List<EntryPoint>());
                    }

                    // Next can be simplified to 1 if (>= 0.250) / else with the actual add after the if else
                    // for now this shows better what is going on
                    if (VortexMath.DistanceKM(groupStartNode, node) < 0.200)
                    {
                        EntryGroups[groupStartNode].Add(new EntryPoint() { OffRunwayNode = edge.StartNode, OnRunwayNode = node, TakeoffLengthRemaining = takeoffLengthRemaining, TurnAngle = entryAngle });
                    }
                    else if (EntryGroups.Count < 2)
                    {
                        // add to new group
                        groupStartNode = node;
                        EntryGroups.Add(node, new List<EntryPoint>());
                        EntryGroups[groupStartNode].Add(new EntryPoint() { OffRunwayNode = edge.StartNode, OnRunwayNode = node, TakeoffLengthRemaining = takeoffLengthRemaining, TurnAngle = entryAngle });
                    }
                    else
                    {
                        foundGroups = true;
                        break;
                    }
                }
                if (foundGroups)
                    break;
            }

            foreach (var result in EntryGroups)
            {
                Console.WriteLine($"{Designator} Group: {result.Key.Id}");

                EntryPoint right = result.Value.Where(ep => ep.TurnAngle < 0).OrderByDescending(ep => ep.TurnAngle).FirstOrDefault();
                EntryPoint left = result.Value.Where(ep => ep.TurnAngle > 0).OrderBy(ep => ep.TurnAngle).FirstOrDefault();
                EntryGroups[result.Key].Clear();

                if (right != null)
                {
                    Console.WriteLine($" Right Entry: {right.OffRunwayNode.Id}->{right.OnRunwayNode.Id} {right.TurnAngle * VortexMath.Rad2Deg:0.0} {right.TakeoffLengthRemaining:0.00}");
                    EntryGroups[result.Key].Add(right);
                }

                if (left != null)
                {
                    Console.WriteLine($" Left  Entry: {left.OffRunwayNode.Id}->{left.OnRunwayNode.Id} {left.TurnAngle * VortexMath.Rad2Deg:0.0} {left.TakeoffLengthRemaining:0.00}");
                    EntryGroups[result.Key].Add(left);
                }
            }         
        }

        private void findExits2()
        {
            ExitGroups.Clear();
            TaxiNode groupStartNode = null;
           
            // First, group all nodes
            foreach (TaxiNode node in RunwayNodes)
            {
                foreach (TaxiEdge edge in node.IncomingEdges)
                {
                    if (edge.IsRunway)
                        continue;

                    if (edge.ReverseEdge == null)
                        continue;

                    TaxiEdge actualEdge = edge.ReverseEdge;
                    double exitAngle = VortexMath.TurnAngle(actualEdge.Bearing, Bearing);

                    if (Math.Abs(exitAngle) > VortexMath.Deg135Rad)
                        continue;

                    if (groupStartNode == null)
                    {
                        groupStartNode = node;
                        ExitGroups.Add(node, new List<ExitPoint>());
                    }

                    double landingLengthUsed = VortexMath.DistanceKM(DisplacedNode, node);  // 'distance used' actually

                    if (VortexMath.DistanceKM(groupStartNode, node) < 0.200)
                    {
                        ExitGroups[groupStartNode].Add(new ExitPoint() { OffRunwayNode = actualEdge.EndNode, OnRunwayNode = node, LandingLengthUsed = landingLengthUsed, TurnAngle = exitAngle });
                    }
                    else
                    {
                        // add to new group
                        groupStartNode = node;
                        ExitGroups.Add(node, new List<ExitPoint>());
                        ExitGroups[groupStartNode].Add(new ExitPoint() { OffRunwayNode = actualEdge.EndNode, OnRunwayNode = node, LandingLengthUsed = landingLengthUsed, TurnAngle = exitAngle });
                    }
                }
            }

            if (ExitGroups.Count == 0)
                return; // todo: add warning

            // Then pick groups based upon distance
            List<ExitPoint> minimumExit = null;
            List<ExitPoint> mediumExit = null;
            List<ExitPoint> longExit = null;
            List<ExitPoint> maxExit = null;

            foreach (KeyValuePair<TaxiNode, List<ExitPoint>> exitGroup in ExitGroups.OrderBy(eg=>eg.Value.First().LandingLengthUsed))
            {
                if (minimumExit == null || minimumExit.First().LandingLengthUsed < VortexMath.Feet4000Km)
                {
                    minimumExit = exitGroup.Value;
                }
                else if (mediumExit == null || mediumExit.First().LandingLengthUsed < VortexMath.Feet6500Km)
                {
                    mediumExit = exitGroup.Value;
                }
                else if (longExit == null || longExit.First().LandingLengthUsed < VortexMath.Feet8000Km)
                {
                    longExit = exitGroup.Value;
                }
                else
                {
                    maxExit = exitGroup.Value;
                }
            }

            ExitGroups.Clear();
            if (minimumExit != null)
                ExitGroups.Add(minimumExit.First().OnRunwayNode, minimumExit);
            if (mediumExit != null)
                ExitGroups.Add(mediumExit.First().OnRunwayNode, mediumExit);
            if (longExit != null)
                ExitGroups.Add(longExit.First().OnRunwayNode, longExit);
            if (maxExit != null)
                ExitGroups.Add(maxExit.First().OnRunwayNode, maxExit);

            foreach (var result in ExitGroups)
            {
                Console.WriteLine($"{Designator} Group: {result.Key.Id}");

                ExitPoint right = result.Value.Where(ep => ep.TurnAngle > 0).OrderBy(ep => ep.TurnAngle).FirstOrDefault();
                ExitPoint left = result.Value.Where(ep => ep.TurnAngle < 0).OrderByDescending(ep => ep.TurnAngle).FirstOrDefault();
                ExitGroups[result.Key].Clear();

                if (right != null)
                {
                    Console.WriteLine($" Right Exit: {right.OnRunwayNode.Id}->{right.OffRunwayNode.Id} {right.TurnAngle * VortexMath.Rad2Deg:0.0} {right.LandingLengthUsed:0.00}");
                    ExitGroups[result.Key].Add(right);
                }

                if (left != null)
                {
                    Console.WriteLine($" Left  Exit: {left.OnRunwayNode.Id}->{left.OffRunwayNode.Id} {left.TurnAngle * VortexMath.Rad2Deg:0.0} {left.LandingLengthUsed:0.00}");
                    ExitGroups[result.Key].Add(left);
                }
            }
        }

        //public class RunwayExitNode
        //{
        //    public double ExitDistance;
        //    public RunwayExit LeftExit;
        //    public RunwayExit RightExit;

        //    public RunwayExitNode()
        //    {
        //        LeftExit = null;
        //        RightExit = null;
        //    }
        //}

        //public class RunwayExit
        //{
        //    public double ExitDistance;
        //    public double ExitAngle;
        //    public double TurnAngle;
        //    public TaxiNode OnRunwayNode;
        //    public TaxiNode OffRunwayNode;

        //    public RunwayExit()
        //    {

        //    }

        //    public override string ToString()
        //    {
        //        return $"{ExitDistance:0.00}km {OnRunwayNode.Id} Turn {ExitAngle * VortexMath.Rad2Deg}";
        //    }

        //}

        //private class ExitLengthComparer : IComparer<RunwayExit>
        //{
        //    public int Compare(RunwayExit x, RunwayExit y)
        //    {
        //        return x.ExitDistance.CompareTo(y.ExitDistance);
        //    }
        //}

        //private void findExits(IEnumerable<TaxiNode> runwayNodes, IEnumerable<TaxiNode> taxiNodes, IEnumerable<TaxiEdge> taxiEdges)
        //{
        //    ExitLengthComparer exitLengthComparer = new ExitLengthComparer();
        //    List<RunwayExit> exitsLeft = new List<RunwayExit>();
        //    List<RunwayExit> exitsRight = new List<RunwayExit>();

        //    // start at long distance
        //    foreach (TaxiNode onRunwayNode in runwayNodes)
        //    {
        //        // Find nodes that have the current runway node in an incoming edge
        //        IEnumerable<TaxiEdge> exitEdges = taxiEdges.Where(edge => edge.StartNode.Id == onRunwayNode.Id);
        //        exitEdges = exitEdges.Where(ee => !runwayNodes.Select(n => n.Id).Contains(ee.EndNode.Id));

        //        foreach (TaxiEdge exit in exitEdges)
        //        {
        //            TaxiNode offRunwayNode = exit.EndNode;
        //            double leaveBearing;

        //            // If there is only one link continuing from the current node use that as link's bearing
        //            IEnumerable<TaxiEdge> secondEdges = taxiEdges.Where(inc => inc.StartNode.Id == offRunwayNode.Id && inc.EndNode.Id != onRunwayNode.Id);
        //            if (secondEdges.Count() == 1)
        //            {
        //                leaveBearing = secondEdges.First().Bearing;
        //            }
        //            else
        //            {
        //                leaveBearing = exit.Bearing;
        //            }

        //            double exitAngle = VortexMath.TurnAngle(Bearing, leaveBearing); // sign indicates left or right turn
        //            if (Math.Abs(exitAngle) > VortexMath.Deg100Rad)
        //                break;

        //            double exitDistance = VortexMath.DistanceKM(this, onRunwayNode);
        //            if (exitDistance < VortexMath.Feet3000Km)
        //                break;

        //            if (exitAngle > 0)
        //                exitsRight.Add(new RunwayExit() { ExitAngle = exitAngle, TurnAngle = Math.Abs(exitAngle), ExitDistance = exitDistance, OnRunwayNode = onRunwayNode, OffRunwayNode = offRunwayNode });
        //            else
        //                exitsLeft.Add(new RunwayExit() { ExitAngle = exitAngle, TurnAngle = Math.Abs(exitAngle), ExitDistance = exitDistance, OnRunwayNode = onRunwayNode, OffRunwayNode = offRunwayNode });
        //        }
        //    }

        //    analyzeExits(exitsLeft);
        //    analyzeExits(exitsRight);
        //}

        //private void analyzeExits(List<RunwayExit> exits)
        //{
        //    if (exits.Count() > 0)
        //    {
        //        exits.Sort(new ExitLengthComparer());

        //        RunwayExit shortExit = exits.First();
        //        RunwayExit mediumExit = null;
        //        RunwayExit longExit = null;
        //        RunwayExit maxExit = null;

        //        foreach (RunwayExit exit in exits)
        //        {
        //            if (exit.ExitDistance <= VortexMath.Feet5000Km && shortExit.TurnAngle > exit.TurnAngle)
        //            {
        //                shortExit = exit;
        //            }
        //            else if (exit != shortExit && exit.ExitDistance > VortexMath.Feet5000Km)
        //            {
        //                if (mediumExit == null)
        //                {
        //                    mediumExit = exit;
        //                }
        //                else if (exit.ExitDistance < VortexMath.Feet6500Km && mediumExit.TurnAngle > exit.TurnAngle)
        //                {
        //                    mediumExit = exit;
        //                }
        //                else if (exit.ExitDistance > VortexMath.Feet6500Km)
        //                {
        //                    if (longExit == null)
        //                    {
        //                        longExit = exit;
        //                    }
        //                    else if (exit.ExitDistance < VortexMath.Feet8000Km && longExit.TurnAngle > exit.TurnAngle)
        //                    {
        //                        longExit = exit;
        //                    }
        //                    else if (exit.ExitDistance > VortexMath.Feet8000Km)
        //                    {
        //                        if (maxExit == null)
        //                        {
        //                            maxExit = exit;
        //                        }
        //                        else if (maxExit.TurnAngle > exit.TurnAngle)
        //                        {
        //                            maxExit = exit;
        //                        }
        //                    }
        //                }
        //            }
        //        }

        //        mergeExit(shortExit);
        //        mergeExit(mediumExit);
        //        mergeExit(longExit);
        //        mergeExit(maxExit);
        //    }
        //}

        //private void mergeExit(RunwayExit exit)
        //{
        //    if (exit == null)
        //        return;

        //    if (!RunwayExits.ContainsKey(exit.OnRunwayNode.Id))
        //    {
        //        RunwayExits[exit.OnRunwayNode.Id] = new RunwayExitNode();
        //    }

        //    RunwayExits[exit.OnRunwayNode.Id].ExitDistance = exit.ExitDistance;
        //    if (exit.ExitAngle < 0)
        //        RunwayExits[exit.OnRunwayNode.Id].LeftExit = exit;
        //    else
        //        RunwayExits[exit.OnRunwayNode.Id].RightExit = exit;
        //}


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
