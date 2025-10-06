using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topology;
using Perlin;

namespace WorldSimulation
{
    public class PerlinDeformer
    {
        static public void Deform(SubregionGraph graph, int frequency, double strength, Random random)
        {
            PerlinNoise.Settings settings = new PerlinNoise.Settings();
            settings.XStitched = true;
            PerlinNoise perlinX = new PerlinNoise(frequency, frequency, settings, random);
            PerlinNoise perlinY = new PerlinNoise(frequency, frequency, settings, random);
            
            foreach (Subregion subregion in graph.Subregions)
            {
                //foreach (Vector2 vertex in subregion.Vertices.Append(subregion.Center))
                //{
                //    Vector2 deformed = _deformVertex(graph, strength, perlinX, perlinY, vertex);
                //    vertex.X = deformed.X;
                //    vertex.Y = deformed.Y;
                //}

                foreach (SubregionEdge edge in subregion.Edges)
                {
                    List<Vector2> vertices = [];
                    vertices.AddRange(edge.Vertices.Select(v => _deformVertex(graph, strength, perlinX, perlinY, v)));
                    if (edge.Center != null) edge.Center = _deformVertex(graph, strength, perlinX, perlinY, edge.Center);
                    edge.ClearVertices();
                    foreach (Vector2 vertex in vertices)
                        edge.AddVertex(vertex);
                }
                subregion.Center = _deformVertex(graph, strength, perlinX, perlinY, subregion.Center);
            }
        }

        private static Vector2 _deformVertex(SubregionGraph graph, double strength, PerlinNoise perlinX, PerlinNoise perlinY, Vector2 vertex)
        {
            double threshold = strength;

            double y = double.Clamp(vertex.Y, 0, graph.Height);

            // deformation strength is 0 at the upper and lower borders of the map, increasing
            // linearly towards thresholds, and constant between them
            double multiplier = 1;
            if (y < threshold)
                multiplier = y / threshold;
            else if (y > graph.Height - threshold)
                multiplier = (graph.Height - y) / threshold;

            double sampleX = perlinX.Sample(vertex.X / graph.Width, y / graph.Width);
            double x = vertex.X + strength * (sampleX - 0.5);

            double sampleY = perlinY.Sample(vertex.X / graph.Width, y / graph.Width);
            y = y + strength * (sampleY - 0.5) * multiplier;

            return new(x, y);
        }
    }
}
