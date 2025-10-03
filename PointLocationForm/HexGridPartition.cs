using Topology;
using PointLocation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PointLocationForm
{
    class HexGridPartition : IRegionPartition<HexCell>
    {
        HexGrid _grid;

        public HexGridPartition(HexGrid grid)
        {
            Top = Math.Round(grid.Height * 2);
            Bottom = Math.Round(-grid.Height);
            Left = Math.Round(-grid.Width);
            Right = Math.Round(grid.Width * 2);
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
