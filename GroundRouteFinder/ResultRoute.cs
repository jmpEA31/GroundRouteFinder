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
        public string LinkName;
        public bool ActiveZone;
        public string ActiveFor;

        public LinkedNode()
        {
            LinkName = "";
            ActiveFor = "";
            ActiveZone = false;
        }
    }

    public class ResultRoute
    {
        public Runway Runway;
        public RunwayTakeOffSpot TakeoffSpot;
        public TaxiNode TargetNode;
        public double Distance;
        public TaxiNode NearestNode;
        public LinkedNode RouteStart;
        public List<int> ValidForSizes;
        public ResultRoute NextSizes;
        public List<TaxiNode> Lead;

        public int MaxSize;
        public int MinSize;

        public ResultRoute(int size)
        {
            ValidForSizes = new List<int>();
            for (int i = 0; i <= size; i++)
                ValidForSizes.Add(i);

            Distance = double.MaxValue;
            NextSizes = null;

            MaxSize = size;
            MinSize = 0;
        }

        public ResultRoute(ResultRoute other)
        {
            TakeoffSpot = other.TakeoffSpot;
            Distance = other.Distance;
            NearestNode = other.NearestNode;
            RouteStart = other.RouteStart;
            ValidForSizes = new List<int>();
            ValidForSizes.AddRange(other.ValidForSizes);
            NextSizes = null;
        }

        internal ResultRoute RouteForSize(int size)
        {
            ResultRoute resultForSize = this;
            while (resultForSize != null)
            {
                if (ValidForSizes.Contains(size))
                    return resultForSize;
                resultForSize = resultForSize.NextSizes;
            }
            return null;
        }

        internal void ImproveResult(ResultRoute better)
        {
            if (this.RouteStart == null || better.ValidForSizes.Max() == this.ValidForSizes.Max())
            {
                // Improves the current size set
                if (this.RouteStart == null)
                {
                    // There was none previously, so the largest size(s) not supported
                    this.ValidForSizes = new List<int>();
                    this.ValidForSizes.AddRange(better.ValidForSizes);

                }
                this.Distance = better.Distance;
                this.RouteStart = better.RouteStart;
                this.NearestNode = better.NearestNode;
                this.TakeoffSpot = better.TakeoffSpot;
            }
            else
            {
                // Improves for a size subset
                if (NextSizes != null)
                    throw new ArgumentException();

                NextSizes = new ResultRoute(better);
                foreach (int size in better.ValidForSizes)
                {
                    ValidForSizes.Remove(size);
                }
            }
        }

        public static ResultRoute ExtractRoute(IEnumerable<TaxiEdge> edges, Runway r, TaxiNode nearestNode, int size)
        {
            ResultRoute extracted = new ResultRoute(size);
            extracted.Runway = r;
            extracted.NearestNode = nearestNode;
            ulong node1 = extracted.NearestNode.Id;
            extracted.Distance = nearestNode.DistanceToTarget;

            TaxiNode pathNode;
            pathNode = nearestNode.PathToTarget;

            TaxiEdge sneakEdge = null;

            if (pathNode != null)
            {
                sneakEdge = edges.SingleOrDefault(e => e.StartNode.Id == node1 && e.EndNode.Id == pathNode.Id);
            }

            extracted.RouteStart = new LinkedNode()
            {
                Node = nearestNode.PathToTarget,
                Next = null,
                LinkName = nearestNode.NameToTarget,
                ActiveZone = (sneakEdge != null) ? sneakEdge.ActiveZone : false,
                ActiveFor = (sneakEdge != null) ? sneakEdge.ActiveFor : "?"
            };

            LinkedNode currentLink = extracted.RouteStart;

            while (pathNode != null)
            {
                ulong node2 = pathNode.Id;
                TaxiEdge edge = edges.Single(e => e.StartNode.Id == node1 && e.EndNode.Id == node2);
                currentLink.Next = new LinkedNode()
                {
                    Node = pathNode.PathToTarget,
                    Next = null,
                    LinkName = pathNode.NameToTarget,
                    ActiveZone = (edge != null) ? edge.ActiveZone : false,
                    ActiveFor = (edge != null) ? edge.ActiveFor : "?",
                };
                node1 = node2;
                currentLink = currentLink.Next;
                pathNode = pathNode.PathToTarget;
            }

            return extracted;
        }
    }
}
