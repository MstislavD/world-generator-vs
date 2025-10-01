using HexGrid;
using System.Drawing;

namespace WorldSimulator
{
    struct VertexData
    {
        public Vertex Vertex;
        public Brush Brush;

        public VertexData(Vertex vertex, Brush brush)
        {
            Vertex = vertex;
            Brush = brush;
        }
    }
}
