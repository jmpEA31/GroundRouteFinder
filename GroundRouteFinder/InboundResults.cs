using System;
using System.Collections.Generic;
using System.IO;
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

        public void AddResult(IEnumerable<TaxiEdge> edges, Runway r, TaxiNode runwayNode, TaxiNode origin, int maxSizeCurrentResult)
        {
            if (runwayNode.Id == 2304 && Parking.Name == "A1")
            {
                int k = 7;
            }

            if (!_results.ContainsKey(runwayNode))
                _results[runwayNode] = new Dictionary<int, ResultRoute>();

            Dictionary<int, ResultRoute> originResults = _results[runwayNode];

            if (originResults.Count == 0)
            {
                originResults.Add(maxSizeCurrentResult, ResultRoute.ExtractRoute(edges, r, origin, maxSizeCurrentResult));
            }
            else
            {
                int minSize = originResults.Min(or => or.Key);
                if (originResults[minSize].Distance > origin.DistanceToTarget)
                {
                    if (minSize > maxSizeCurrentResult)
                    {
                        originResults.Add(maxSizeCurrentResult, ResultRoute.ExtractRoute(edges, r, origin, maxSizeCurrentResult));
                        originResults[minSize].MinSize = (maxSizeCurrentResult + 1);
                    }
                    else if (minSize == maxSizeCurrentResult)
                    {

                        originResults[minSize] = ResultRoute.ExtractRoute(edges, r, origin, maxSizeCurrentResult);
                    }
                }
            }
        }

        public void WriteRoutes()
        {
            foreach (KeyValuePair<TaxiNode, Dictionary<int, ResultRoute>> sizeRoutes in _results)
            {
                for (int size = Parking.MaxSize; size >= 0; size--)
                {
                    if (sizeRoutes.Value.ContainsKey(size))
                    {
                        ResultRoute route = sizeRoutes.Value[size];
                        if (route.MinSize > Parking.MaxSize)
                            continue;

                        List<int> routeSizes = new List<int>();
                        for (int s = route.MinSize; s<=route.MaxSize; s++)
                        {
                            routeSizes.AddRange(Settings.XPlaneCategoryToWTType(s));
                        }

                        string allSizes = string.Join(" ", routeSizes.OrderBy(w => w));

                        string sizeName = (routeSizes.Count == 10) ? "all" : allSizes.Replace(" ", "");
                        string fileName = $"E:\\GroundRoutes\\Arrivals\\LFPG\\{route.Runway.Designator}_to_{Parking.FileNameSafeName}-{sizeRoutes.Key.Id}_{sizeName}.txt";
                        File.Delete(fileName);
                        using (StreamWriter sw = File.CreateText(fileName))
                        {
                            sw.WriteLine($"{route.Distance * 1000}");
                        }
                    }
                }
            }
        }
    }
}
