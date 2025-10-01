using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topology
{
    public interface INeighbors<T>
    {
        IEnumerable<T> Neighbors { get; }
    }

    public class HexCell : INeighbors<HexCell>
    {
        Vertex[] _vertices = new Vertex[6];
        HexCell[] _neighbors = new HexCell[6];
        Edge[] _edges = new Edge[6];

        public Vertex Center { get; set; }
        public void AddNeighbor(HexCell cell, int direction) => _neighbors[direction] = cell;
        public void AddVertex(Vertex vertex, int direction) => _vertices[direction] = vertex;
        public void AddEdge(Edge edge, int direction) => _edges[direction] = edge;
        public HexCell GetNeighbor(int direction) => _neighbors[direction % 6];
        public Vertex GetVertex(int direction) => _vertices[direction % 6];
        public Edge GetEdge(int direction) => _edges[direction % 6];
        public IEnumerable<Vertex> Vertices => _vertices;
        public IEnumerable<HexCell> Neighbors => _neighbors.Where(n => n != null);
        public IEnumerable<Edge> Edges => _edges;
        public Edge GetEdgeByNeighbor(HexCell neighbor) => _edges.First(e => e.Cell1 == neighbor || e.Cell2 == neighbor);
        public int GetDirection(Edge edge) => _edges.ToList().IndexOf(edge);
        public int GridPositionX { get; set; }
        public int GridPositionY { get; set; }

    }
}
