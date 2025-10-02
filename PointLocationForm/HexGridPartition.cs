using Topology;
using PointLocation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PointLocationForm
{
    class HexGridPartition : IRegionPartition<HexCell>
    {
        Grid _grid;

        public HexGridPartition(Grid grid)
        {
            Top = Math.Round(grid.YDimension * 2);
            Bottom = Math.Round(-grid.YDimension);
            Left = Math.Round(-grid.XDimension);
            Right = Math.Round(grid.XDimension * 2);
            _grid = grid;
        }

        public double Top { get; set; }

        public double Bottom { get; set; }

        public double Left { get; set; }

        public double Right { get; set; }

        public double Epsilon => 0;

        IEnumerable<HexCell> IRegionPartition<HexCell>.Regions => _grid.Cells;

        public IEnumerable<Edge> Edges(HexCell region) => region.Edges.Select(_rounded);

        Edge _rounded(Edge edge)
        {
            int precision = 1;
            double x1 = Math.Round(edge.Vertex1.X, precision);
            double y1 = Math.Round(edge.Vertex1.Y, precision);
            double x2 = Math.Round(edge.Vertex2.X, precision);
            double y2 = Math.Round(edge.Vertex2.Y, precision);
            return new Edge() { Vertex1 = new Vector2(x1, y1), Vertex2 = new Vector2(x2, y2) };
        }
    }
}
