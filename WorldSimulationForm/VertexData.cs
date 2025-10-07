using Topology;
using System.Drawing;

namespace WorldSimulationForm
{
    struct VertexData
    {
        public Vector2 Vertex;
        public Brush Brush;

        public VertexData(Vector2 vertex, Brush brush)
        {
            Vertex = vertex;
            Brush = brush;
        }
    }
}
