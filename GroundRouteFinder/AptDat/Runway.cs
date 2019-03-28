using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GroundRouteFinder.LogSupport;

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
                    VortexMath.PointFrom(Latitude, Longitude, Bearing, Displacement, ref DisplacedLatitude, ref DisplacedLongitude);
                    Length = VortexMath.DistanceKM(DisplacedLatitude, DisplacedLongitude, _oppositeEnd.Latitude, _oppositeEnd.Longitude);
                }
            }
        }

        public double Length;

        public List<TaxiNode> RunwayNodes;
        public Dictionary<TaxiNode, List<EntryPoint>> EntryGroups;
        public Dictionary<TaxiNode, List<ExitPoint>> ExitGroups;

        public double Bearing { get; set; }


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

            Designator = designator;
            Displacement = displacement;
            DisplacedLatitude = 0;
            DisplacedLongitude = 0;

            OppositeEnd = null;
        }

        public bool Analyze(IEnumerable<TaxiNode> taxiNodes, IEnumerable<TaxiEdge> taxiEdges)
        {
            Logger.Log($"---------------------------------------------");
            Logger.Log($"Analyzing Runway {Designator}");
            Logger.Log($"---------------------------------------------");

            // Find the taxi nodes closest to the start of the runway and the displaced start
            double shortestDistance = double.MaxValue;
            double shortestDisplacedDistance = double.MaxValue;

            IEnumerable<TaxiNode> runwayNodes = taxiEdges.Where(te=>te.IsRunway).Select(te => te.StartNode).Concat(taxiEdges.Where(te => te.IsRunway).Select(te => te.EndNode)).Distinct();
            foreach (TaxiNode node in runwayNodes)
            {
                // Start with a few sanity cehcks to prevent runways without nodes in the apt.dat fro picking up nodes from nearby runways
                double angleFromStart = VortexMath.AbsTurnAngle(Bearing, VortexMath.BearingRadians(this, node));
                double d = VortexMath.DistanceKM(node.Latitude, node.Longitude, DisplacedLatitude, DisplacedLongitude);

                // A node more than 10m from the runway start should be near the centerline to be accepted
                if (d > 0.010 && angleFromStart > VortexMath.Deg005Rad)
                    continue;

                // Ignore node that are farther away from the runway coordinates than the length of the runway
                if (d > Length * 1.1) 
                        continue;

                // Now see if this node is better than the best so far
                if (d < shortestDisplacedDistance)
                {
                    shortestDisplacedDistance = d;
                    DisplacedNode = node;
                }

                d = VortexMath.DistanceKM(this, node);
                if (d < shortestDistance)
                {
                    shortestDistance = d;
                    NearestNode = node;
                }
            }

            if (NearestNode == null)
            {
                // The KOKC runway 18 clause...
                Logger.Log($"No suitable nodes on the runway found.");
                AvailableForTakeOff = false;
                AvailableForLanding = false;
                return false;
            }

            // Find the nodes that make up this runway: first find an edge connected to the nearest node
            IEnumerable<TaxiEdge> selectedEdges = taxiEdges.Where(te => te.IsRunway && (te.EndNode.Id == NearestNode.Id || te.StartNode.Id == NearestNode.Id));
            if (selectedEdges.Count() == 0)
            {
                Logger.Log($"No runway edges found.");
                AvailableForTakeOff = false;
                AvailableForLanding = false;
                return false;
            }

            // The name of the link gives the name of the runway, use it to retrieve all edges for this runway
            string edgeKey = selectedEdges.First().LinkName;
            selectedEdges = taxiEdges.Where(te => te.LinkName == edgeKey);

            RunwayNodes = FindNodeChain(taxiNodes, selectedEdges);

            if (AvailableForTakeOff)
                FindEntries();
            else
                Logger.Log("Not in use for take offs");

            if (AvailableForLanding)
                FindExits();
            else
                Logger.Log("Not in use for landing");
            
            return true;
        }

        private void FindEntries()
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
                Logger.Log($"{Designator} Group: {result.Key.Id}");

                EntryPoint right = result.Value.Where(ep => ep.TurnAngle < 0).OrderByDescending(ep => ep.TurnAngle).FirstOrDefault();
                EntryPoint left = result.Value.Where(ep => ep.TurnAngle > 0).OrderBy(ep => ep.TurnAngle).FirstOrDefault();
                EntryGroups[result.Key].Clear();

                if (right != null)
                {
                    Logger.Log($" Right Entry: {right.OffRunwayNode.Id}->{right.OnRunwayNode.Id} {right.TurnAngle * VortexMath.Rad2Deg:0.0} {right.TakeoffLengthRemaining * VortexMath.KmToFoot:0}ft");
                    EntryGroups[result.Key].Add(right);
                }

                if (left != null)
                {
                    Logger.Log($" Left  Entry: {left.OffRunwayNode.Id}->{left.OnRunwayNode.Id} {left.TurnAngle * VortexMath.Rad2Deg:0.0} {left.TakeoffLengthRemaining * VortexMath.KmToFoot:0}ft");
                    EntryGroups[result.Key].Add(left);
                }
            }         
        }

        private void FindExits()
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
                Logger.Log($"{Designator} Group: {result.Key.Id}");

                ExitPoint right = result.Value.Where(ep => ep.TurnAngle > 0).OrderBy(ep => ep.TurnAngle).FirstOrDefault();
                ExitPoint left = result.Value.Where(ep => ep.TurnAngle < 0).OrderByDescending(ep => ep.TurnAngle).FirstOrDefault();
                ExitGroups[result.Key].Clear();

                if (right != null)
                {
                    Logger.Log($" Right Exit: {right.OnRunwayNode.Id}->{right.OffRunwayNode.Id} {right.TurnAngle * VortexMath.Rad2Deg:0.0} {right.LandingLengthUsed * VortexMath.KmToFoot:0}ft");
                    ExitGroups[result.Key].Add(right);
                }

                if (left != null)
                {
                    Logger.Log($" Left  Exit: {left.OnRunwayNode.Id}->{left.OffRunwayNode.Id} {left.TurnAngle * VortexMath.Rad2Deg:0.0} {left.LandingLengthUsed * VortexMath.KmToFoot:0}ft");
                    ExitGroups[result.Key].Add(left);
                }
            }
        }

        /// <summary>
        /// Find the chain of TaxiNodes that represent this runway
        /// </summary>
        /// <param name="taxiNodes"></param>
        /// <param name="taxiEdges"></param>
        /// <returns></returns>
        private List<TaxiNode> FindNodeChain(IEnumerable<TaxiNode> taxiNodes, IEnumerable<TaxiEdge> taxiEdges)
        {
            List<TaxiNode> nodes = new List<TaxiNode>();

            // Start with the node nearest to the runway lat/lon
            TaxiNode currentNode = NearestNode;
            nodes.Add(currentNode);
            ulong previousNodeId = 0;

            do
            {
                // Now find an edge that is marked as 'runway' and that starts at the current node, but does not lead to the previous node
                IEnumerable<TaxiEdge> edgesToNext = taxiEdges.Where(e => e.IsRunway && (e.StartNode.Id == currentNode.Id && e.EndNode.Id != previousNodeId));
                if (edgesToNext.Count() == 0)
                    break;

                TaxiEdge edgeToNext = edgesToNext.First();
                if (edgesToNext.Count() > 1)
                {
                    double maxDeviation = double.MaxValue;

                    foreach (TaxiEdge candidate in edgesToNext)
                    {
                        double deviation = VortexMath.TurnAngle(this.Bearing, VortexMath.BearingRadians(currentNode, candidate.EndNode));
                        if (deviation < maxDeviation)
                        {
                            edgeToNext = candidate;
                            maxDeviation = deviation;
                        }
                    }
                }

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
