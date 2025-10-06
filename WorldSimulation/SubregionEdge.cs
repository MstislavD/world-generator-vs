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
        public SubregionEdge PrevEdge { get; set; }
        public SubregionEdge NextEdge { get; set; }
        public Vector2 Center { get; set; }

        public void RecalculateCenter() => Center = _vertices[(_vertices.Count + 1) / 2];
        public IEnumerable<Edge> Edges
        {
            get
            {
                for (int i = 1; i < _vertices.Count; i++)
                {
                    yield return new Edge() { Vertex1 = _vertices[i - 1], Vertex2 = _vertices[i] };
                }
                yield return new Edge() { Vertex1 = _vertices[_vertices.Count - 1], Vertex2 = _vertices[0] };
            }
        }
    }
}
