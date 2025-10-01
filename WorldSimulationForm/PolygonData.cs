using System.Collections.Generic;
using HexGrid;
using System.Drawing;

namespace WorldSimulator
{
    struct PolygonData
    {
        public IEnumerable<Vertex> Vertices;
        public Brush Brush;

        public PolygonData(IEnumerable<Vertex> vertices, Brush brush)
        {
            Vertices = vertices;
            Brush = brush;
        }
        public PolygonData(IEnumerable<Vertex> vertices, TextureBrush brush)
        {
            Vertices = vertices;
            brush.WrapMode = System.Drawing.Drawing2D.WrapMode.Tile;
            Brush = brush;
        }        public PolygonData(IEnumerable<Vertex> vertices, Color color) : this (vertices, new SolidBrush(color)) { }
        public PolygonData(HexCell cell, Color color) : this(cell.Vertices, new SolidBrush(color)) { }
        public PolygonData(HexCell cell, Brush brush) : this(cell.Vertices, brush) { }
    }
}
