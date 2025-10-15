using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topology;
using TrapezoidSpatialIndex;

namespace WorldSimulation
{
    class SubregionPartition : IRegionPartition<Subregion>
    {
        SubregionGraph _graph;

        public SubregionPartition(SubregionGraph graph)
        {
            Top = 2 * graph.Height;
            Bottom = -graph.Height;
            Left = -graph.Width;
            Right = 2 * graph.Width;

            _graph = graph;
        }
        public IEnumerable<Subregion> Regions => _graph.Subregions;

        public double Top { get; set; }

        public double Bottom { get; set; }

        public double Left { get; set; }

        public double Right { get; set; }

        public double Epsilon => 0;

        public IEnumerable<LineSegment> Edges(Subregion region)
        {
            List<Vector2> vertices = region.Vertices.ToList();
            for (int i = 1; i < vertices.Count; i++)
            {
                yield return _rounded(new Edge() { Vertex1 = vertices[i - 1], Vertex2 = vertices[i] });
            }
            yield return _rounded(new Edge() { Vertex1 = vertices[vertices.Count - 1], Vertex2 = vertices[0] });
        }

        Edge _rounded(Edge edge)
        {
            int precision = 5;
            double x1 = Math.Round(edge.Vertex1.X, precision);
            double y1 = Math.Round(edge.Vertex1.Y, precision);
            double x2 = Math.Round(edge.Vertex2.X, precision);
            double y2 = Math.Round(edge.Vertex2.Y, precision);
            return new Edge() { Vertex1 = new Vector2(x1, y1), Vertex2 = new Vector2(x2, y2) };
        }
    }
}
