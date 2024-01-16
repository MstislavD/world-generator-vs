using System.Collections.Generic;
using HexGrid;

namespace WorldSim
{
    public class SubregionEdge
    {
        List<Vertex> _vertices = new List<Vertex>();
        public void AddVertex(Vertex vertex) => _vertices.Add(vertex);
        public IEnumerable<Vertex> Vertices => _vertices;
        public void ClearVertices() => _vertices = new List<Vertex>();
        public SubregionEdge PrevEdge { get; set; }
        public SubregionEdge NextEdge { get; set; }
        public Vertex Center { get; set; }
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
