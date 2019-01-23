using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GroundRouteFinder.AptDat;

namespace GroundRouteFinder
{
    public class InboundResults
    {
        public Parking Parking;

        private Dictionary<TaxiNode, Dictionary<int, ResultRoute>> _results;

        public InboundResults(Parking parking)
        {
            Parking = parking;
            _results = new Dictionary<TaxiNode, Dictionary<int, ResultRoute>>();
        }

        public void AddResult(IEnumerable<TaxiEdge> edges, TaxiNode origin, int maxSizeCurrentResult)
        {
            if (!_results.ContainsKey(origin))
                _results[origin] = new Dictionary<int, ResultRoute>();

            Dictionary<int, ResultRoute> originResults = _results[origin];

            if (originResults.Count == 0)
            {
                originResults.Add(maxSizeCurrentResult, ResultRoute.ExtractRoute(edges, origin, maxSizeCurrentResult));
            }
            else
            {
                int minSize = originResults.Min(or => or.Key);
                if (minSize > maxSizeCurrentResult)
                {
                    originResults.Add(maxSizeCurrentResult, ResultRoute.ExtractRoute(edges, origin, maxSizeCurrentResult));
                    originResults[minSize].MinSize = (maxSizeCurrentResult + 1);
                }
                else if (minSize == maxSizeCurrentResult)
                {
                    if (originResults[minSize].Distance > origin.DistanceToTarget)
                    {
                        originResults[minSize] = ResultRoute.ExtractRoute(edges, origin, maxSizeCurrentResult);
                    }
                }
            }
        }

        public ResultRoute GetRoute(TaxiNode origin, int size)
        {
            if (!_results.ContainsKey(origin))
                return null;

            for (int s = size; s < TaxiNode.Sizes; s++)
            {
                if (_results[origin].ContainsKey(s))
                    return _results[origin][s];
            }
            return null;
        }

        public void WriteRoutes()
        {

        }
    }
}
