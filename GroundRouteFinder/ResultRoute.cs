using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GroundRouteFinder.AptDat;

namespace GroundRouteFinder
{
    public class LinkedNode
    {
        public TaxiNode Node;
        public LinkedNode Next;
        public TaxiEdge Edge;

        public LinkedNode()
        {
        }
    }

    public class ResultRoute
    {
        public Runway Runway;
        public double AvailableRunwayLength;
        public List<Parking> Parkings;

        public RunwayTakeOffSpot TakeoffSpot;
        public TaxiNode TargetNode;
        public double Distance;
        public TaxiNode NearestNode;
        public LinkedNode RouteStart;

        public XPlaneAircraftCategory MaxSize;
        public XPlaneAircraftCategory MinSize;

        public ResultRoute(XPlaneAircraftCategory size)
        {
            Parkings = new List<Parking>();

            Distance = double.MaxValue;

            MaxSize = size;
            MinSize = 0;
        }

        public void AddParking(Parking parking)
        {
            if (!Parkings.Contains(parking))
            {
                Parkings.Add(parking);
            }
        }

        /// <summary>
        /// Extract the route that starts at TaxiNode 'startNode'
        /// </summary>
        /// <param name="edges">A list of all available edges</param>
        /// <param name="startNode">The first node of the route</param>
        /// <param name="size">The maximum size for which this route is valid</param>
        /// <returns>The route as a linked list of nodes with additional informationthat will be needed when writing the route to a file</returns>
        public static ResultRoute ExtractRoute(IEnumerable<TaxiEdge> edges, TaxiNode startNode, XPlaneAircraftCategory size)
        {
            ResultRoute extracted = new ResultRoute(size);
            extracted.Runway = null;
            extracted.NearestNode = startNode;
            ulong node1 = extracted.NearestNode.Id;
            extracted.Distance = startNode.DistanceToTarget;

            TaxiNode pathNode;
            pathNode = startNode.NextNodeToTarget;

            TaxiEdge sneakEdge = null;

            if (pathNode != null)
            {
                sneakEdge = edges.SingleOrDefault(e => e.StartNode.Id == node1 && e.EndNode.Id == pathNode.Id);
            }

            // Set up the first link
            extracted.RouteStart = new LinkedNode()
            {
                Node = startNode.NextNodeToTarget,
                Next = null,
                Edge = sneakEdge
            };

            LinkedNode currentLink = extracted.RouteStart;

            // And follow the path...
            while (pathNode != null)
            {
                double currentBearing = currentLink.Node.BearingToTarget;
                ulong node2 = pathNode.Id;
                TaxiEdge edge = edges.Single(e => e.StartNode.Id == node1 && e.EndNode.Id == node2);

                if (pathNode.NextNodeToTarget != null && pathNode.NextNodeToTarget.DistanceToTarget > 0)
                {
                    double nextBearing = pathNode.NextNodeToTarget.BearingToTarget;
                    double turn = VortexMath.AbsTurnAngle(currentBearing, nextBearing);
/*
                    // This filters out very sharp turns if an alternate exists in exchange for a longer route:
                    // todo: parameters. Now => if more than 120 degrees and alternate < 45 exists use alternate
                    if (turn > VortexMath.Deg120Rad)
                    {
                        IEnumerable<TaxiEdge> altEdges = edges.Where(e => e.StartNode.Id == pathNode.NextNodeToTarget.Id &&
                                                                          e.EndNode.Id != pathNode.NextNodeToTarget.NextNodeToTarget.Id &&
                                                                          e.EndNode.Id != pathNode.Id);

                        foreach (TaxiEdge te in altEdges)
                        {
                            if (te.EndNode.DistanceToTarget < double.MaxValue)
                            {
                                double newTurn = VortexMath.AbsTurnAngle(currentBearing, te.EndNode.BearingToTarget);
                                if (newTurn < VortexMath.Deg100Rad)
                                {
                                    // Fiddling with Dijkstra results like this may generate a loop in the route
                                    // So scan it before actually using the reroute
                                    if (!hasLoop(te.EndNode, pathNode))
                                    {
                                        pathNode.NextNodeToTarget.OverrideToTarget = te.EndNode;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else if (turn > VortexMath.Deg005Rad)   // Any turn larger than 5 degrees: if going straight does not lead to more than 250m extra distance... go straight.
                    {
                        IEnumerable<TaxiEdge> altEdges = edges.Where(e => e.StartNode.Id == pathNode.NextNodeToTarget.Id &&
                                                  e.EndNode.Id != pathNode.NextNodeToTarget.NextNodeToTarget.Id &&
                                                  e.EndNode.Id != pathNode.Id);

                        foreach (TaxiEdge te in altEdges)
                        {
                            if (te.EndNode.DistanceToTarget < (pathNode.NextNodeToTarget.NextNodeToTarget.DistanceToTarget + 0.250))
                            {
                                double newTurn = VortexMath.AbsTurnAngle(currentBearing, te.EndNode.BearingToTarget);
                                if (newTurn < VortexMath.Deg005Rad)
                                {
                                    // Fiddling with Dijkstra results like this may generate a loop in the route
                                    // So scan it before actually using the reroute
                                    if (!hasLoop(te.EndNode, pathNode))
                                    {
                                        pathNode.NextNodeToTarget.OverrideToTarget = te.EndNode;
                                        break;
                                    }
                                }
                            }
                        }
                    }
*/
                }

                TaxiNode nextNode = (pathNode.OverrideToTarget != null) ? pathNode.OverrideToTarget : pathNode.NextNodeToTarget;

                currentLink.Next = new LinkedNode()
                {
                    Node = nextNode,
                    Next = null,
                };
                node1 = node2;

                currentLink.Edge = edge;

                currentLink = currentLink.Next;
                extracted.TargetNode = pathNode;

                pathNode.OverrideToTarget = null;
                pathNode = nextNode;
            }
            
            return extracted;
        }

        private static bool hasLoop(TaxiNode startNode, TaxiNode loopNode)
        {
            while (startNode != null)
            {
                if (startNode == loopNode.NextNodeToTarget)
                    return true;
                startNode = startNode.NextNodeToTarget;
            }
            return false;
        }
    }
}
