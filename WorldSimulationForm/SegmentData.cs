using System.Collections.Generic;
using HexGrid;
using System.Drawing;

namespace WorldSimulator
{
    struct SegmentData
    {
        public IEnumerable<Vertex> Vertices;
        public Pen Pen;

        public SegmentData(IEnumerable<Vertex> vertices, Color color, int width)
        {
            Vertices = vertices;
            Pen = new Pen(color, width);
            Pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            Pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
        }
        public SegmentData(IEnumerable<Vertex> vertices, Color color) : this(vertices, color, 1) { }
        public SegmentData(IEnumerable<Vertex> vertices, Pen pen)
        {
            Vertices = vertices;
            Pen = pen;
        }
        public SegmentData(Edge edge, Pen pen): this (new Vertex[] { edge.Vertex1, edge.Vertex2 }, pen) { }
    }
}
