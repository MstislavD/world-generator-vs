using System.Collections.Generic;
using Topology;
using System.Drawing;

namespace WorldSimulationForm
{
    using HexGrid = WorldSimulation.WorldGrid;
    using HexCell = WorldSimulation.WorldCell;
    using Edge = WorldSimulation.WorldEdge;
    struct SegmentData
    {
        public IEnumerable<Vector2> Vertices;
        public Pen Pen;

        public SegmentData(IEnumerable<Vector2> vertices, Color color, int width)
        {
            Vertices = vertices;
            Pen = new Pen(color, width);
            Pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            Pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
        }
        public SegmentData(IEnumerable<Vector2> vertices, Color color) : this(vertices, color, 1) { }
        public SegmentData(IEnumerable<Vector2> vertices, Pen pen)
        {
            Vertices = vertices;
            Pen = pen;
        }
        public SegmentData(Edge edge, Pen pen): this (new Vector2[] { edge.Vertex1, edge.Vertex2 }, pen) { }
    }
}
