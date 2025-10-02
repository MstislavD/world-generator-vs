using Topology;
using PointLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PointLocationForm
{

    class RegionPartition : IRegionPartition<Region>
    {
        List<Region> _regions;

        public RegionPartition(IEnumerable<Region> regions, double top, double bottom, double left, double right)
        {
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
            _regions = regions.ToList();
        }

        public IEnumerable<Region> Regions => _regions;

        public double Top { get; set; }

        public double Bottom { get; set; }

        public double Left { get; set; }

        public double Right { get; set; }

        public double Epsilon => 0;

        //public IEnumerable<Edge> Edges(Region region)
        //{
        //    return region.Edges;
        //}

        public IEnumerable<Edge> Edges(Region region)
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
            int precision = 10;
            double x1 = Math.Round(edge.Vertex1.X, precision);
            double y1 = Math.Round(edge.Vertex1.Y, precision);
            double x2 = Math.Round(edge.Vertex2.X, precision);
            double y2 = Math.Round(edge.Vertex2.Y, precision);
            return new Edge() { Vertex1 = new Vector2(x1, y1), Vertex2 = new Vector2(x2, y2) };
        }
    }
}
