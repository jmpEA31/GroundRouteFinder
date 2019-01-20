using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public RunwayTakeOffSpot TakeoffSpot;
        public TaxiNode TargetNode;
        public double Distance;
        public TaxiNode NearestNode;
        public LinkedNode RouteStart;
        public List<int> ValidForSizes;
        public ResultRoute NextSizes;

        public ResultRoute(int size)
        {
            ValidForSizes = new List<int>();
            for (int i = 0; i <= size; i++)
                ValidForSizes.Add(i);

            Distance = double.MaxValue;
            NextSizes = null;
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
    }
}
