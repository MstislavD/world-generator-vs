using System.Collections.Generic;
using Topology;
using System.Drawing;

namespace WorldSimulator
{
    struct PolygonData
    {
        public IEnumerable<Vector2> Vertices;
        public Brush Brush;

        public PolygonData(IEnumerable<Vector2> vertices, Brush brush)
        {
            Vertices = vertices;
            Brush = brush;
        }
        public PolygonData(IEnumerable<Vector2> vertices, TextureBrush brush)
        {
            Vertices = vertices;
            brush.WrapMode = System.Drawing.Drawing2D.WrapMode.Tile;
            Brush = brush;
        }        public PolygonData(IEnumerable<Vector2> vertices, Color color) : this (vertices, new SolidBrush(color)) { }
        public PolygonData(HexCell cell, Color color) : this(cell.Vertices, new SolidBrush(color)) { }
        public PolygonData(HexCell cell, Brush brush) : this(cell.Vertices, brush) { }
    }
}
