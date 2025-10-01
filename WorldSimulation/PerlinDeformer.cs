using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexGrid;
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

            double transitionBand = strength;

            foreach (Subregion subregion in graph.Subregions)
            {
                foreach (Vertex vertex in subregion.Vertices.Append(subregion.Center))
                {
                    vertex.Y = Math.Max(vertex.Y, 0);
                    vertex.Y = Math.Min(vertex.Y, graph.Height);

                    double transitionMultiplier = 1;
                    if (vertex.Y < transitionBand)
                        transitionMultiplier = vertex.Y / transitionBand;
                    else if (vertex.Y > graph.Height - transitionBand)
                        transitionMultiplier = (graph.Height - vertex.Y) / transitionBand;

                    double sampleX = perlinX.Sample(vertex.X / graph.Width, vertex.Y / graph.Width);
                    double x = vertex.X + strength * (sampleX - 0.5);
                    vertex.X = x;

                    double sampleY = perlinY.Sample(vertex.X / graph.Width, vertex.Y / graph.Width);
                    double y = vertex.Y + strength * (sampleY - 0.5) * transitionMultiplier;
                    vertex.Y = y;
                }
            }

           
        }
    }
}
