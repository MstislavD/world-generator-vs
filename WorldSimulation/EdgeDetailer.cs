using Topology;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldSimulation
{
    public class EdgeDetailer
    {
        static public void Detail(WorldSubregionGraph graph, double threshold)
        {
            foreach (SubregionEdge edge in graph.Subregions.SelectMany(subregion => subregion.Edges))
            {
                List<Vector2> edgeVertices = edge.Vertices.ToList();
                edge.ClearVertices();
                Vector2 previousVertex = null;
                foreach (Vector2 vertex in edgeVertices)
                {
                    if (previousVertex != null)
                    {
                        double distance = Vector2.Distance(vertex, previousVertex);
                        int newVerticesCount = (int)(distance / threshold);
                        for (int i = 0; i < newVerticesCount - 1; i++)
                        {
                            double delta = (i + 1) / (double)newVerticesCount;
                            Vector2 v = Vector2.Lerp(previousVertex, vertex, delta);
                            edge.AddVertex(v);
                        }
                    }
                    edge.AddVertex(vertex);
                    previousVertex = vertex;
                }
            }
        }
    }
}
