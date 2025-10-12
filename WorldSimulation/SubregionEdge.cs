using System.Collections.Generic;
using Topology;

namespace WorldSimulation
{
    public class SubregionEdge
    {
        List<Vector2> _vertices = new List<Vector2>();
        public void AddVertex(Vector2 vertex) => _vertices.Add(vertex);
        public IEnumerable<Vector2> Vertices => _vertices;
        public void ClearVertices() => _vertices = new List<Vector2>();
        public Vector2? Center { get; set; }
    }
}
