using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder
{
    public class RunwayEdges
    {
        public List<RunwayEdge> Edges;

        public RunwayEdges()
        {
            Edges = new List<RunwayEdge>();
        }

        public void AddEdge(TaxiNode v1, TaxiNode v2)
        {
            Edges.Add(new RunwayEdge(v1, v2));
        }

        public bool HasVertex(ulong vertexId)
        {
            return Edges.Any(e => e.V1.Id == vertexId || e.V2.Id == vertexId);
        }

        public void Process()
        {
            
        }

        public string FindChainFrom(ulong vertexId)
        {
            StringBuilder sb = new StringBuilder();
            RunwayEdge edge = Edges.SingleOrDefault(e => e.V1.Id == vertexId || e.V2.Id == vertexId);
            if (edge == null)
            {
                return "No or multiple edges with start node found.";
            }
            else
            {
                ulong previousId = vertexId;
                TaxiNode next = (edge.V1.Id == previousId) ? edge.V2 : edge.V1;
                ulong nextId = next.Id;
                sb.AppendFormat("{0}* ", previousId);
                sb.AppendFormat("{0}{1} ", nextId, next.IsNonRunwayEdge ? "*":" ");

                while (edge != null)
                {
                    edge = Edges.SingleOrDefault(e => (e.V1.Id == nextId || e.V2.Id == nextId) && e.V1.Id != previousId && e.V2.Id != previousId);
                    if (edge != null)
                    {
                        previousId = nextId;
                        next = (edge.V1.Id == previousId) ? edge.V2 : edge.V1;
                        nextId = next.Id;
                        sb.AppendFormat("{0}{1} ", nextId, next.IsNonRunwayEdge ? "*" : " ");
                    }
                }
                return sb.ToString();
            }
        }
    }
}
