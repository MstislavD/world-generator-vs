using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Topology;

namespace Topology
{
    /// <summary>
    /// Representaion of a hexagonal tile in a hexagonal grid. Each tile has six vertices and is connected to its neighbors through edges.
    /// </summary>
    /// <typeparam name="TCell"></typeparam>
    /// <typeparam name="TEdge"></typeparam>
    public class HexCell<TCell, TEdge> : INode<TCell>, INode<TCell, TEdge>, IPolygon
    {
        BoundingBox? _bbox = null;
        Vector2[] _vertices = new Vector2[6];
        // To decrease memory consumption it is possible to remove neighbor list and infere neighbours via edges.
        TCell[] _neighbors = new TCell[6];
        TEdge[] _edges = new TEdge[6];

        public Vector2 Center { get; set; }
        public void AddNeighbor(TCell cell, int direction) => _neighbors[direction] = cell;
        public void SetVertex(Vector2 vertex, int direction) => _vertices[direction] = vertex;
        public void AddEdge(TEdge edge, int direction) => _edges[direction] = edge;
        public TCell GetNeighbor(int direction) => _neighbors[direction % 6];
        public Vector2 GetVertex(int direction) => _vertices[direction % 6];
        public TEdge GetEdge(int direction) => _edges[direction % 6];
        public IEnumerable<Vector2> Vertices => _vertices;
        public IEnumerable<TCell> Neighbors => _neighbors.Where(n => n != null);
        public IEnumerable<TEdge> Edges => _edges;
        public int VertexCount => 6;
        public TEdge GetEdgeByNeighbor(TCell neighbor) => _edges[Array.IndexOf(_neighbors, neighbor)];
        public int GetDirection(TEdge edge) => _edges.ToList().IndexOf(edge);
        public BoundingBox Bounds => _bbox ?? (_bbox = Polygon.CalculateBoundingBox(this));
    }

    /// <summary>
    /// Concrete implementation of the generic HexCell class.
    /// </summary>
    public class HexCell : HexCell<HexCell, Edge> { }
}
