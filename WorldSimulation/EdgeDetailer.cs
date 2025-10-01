using HexGrid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldSimulation
{
    public class EdgeDetailer
    {
        static public void Detail(SubregionGraph graph, double threshold)
        {
            foreach (SubregionEdge edge in graph.Subregions.SelectMany(subregion => subregion.Edges))
            {
                List<Vertex> edgeVertices = edge.Vertices.ToList();
                edge.ClearVertices();
                Vertex previousVertex = null;
                foreach (Vertex vertex in edgeVertices)
                {
                    if (previousVertex != null)
                    {
                        double distance = Vertex.Distance(vertex, previousVertex);
                        int newVerticesCount = (int)(distance / threshold);
                        for (int i = 0; i < newVerticesCount - 1; i++)
                        {
                            double delta = (i + 1) / (double)newVerticesCount;
                            Vertex v = Vertex.Between(previousVertex, vertex, delta);
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
