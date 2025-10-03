using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topology;

namespace Topology
{
    public interface INeighbors<T>
    {
        IEnumerable<T> Neighbors { get; }
    }

    /// <summary>
    /// Representaion of a hexagonal tile in a hexagonal grid. Each tile has six vertices and is connected to its neighbors through edges.
    /// </summary>
    /// <typeparam name="TCell"></typeparam>
    /// <typeparam name="TEdge"></typeparam>
    public class HexCell<TCell, TEdge> : INeighbors<TCell>
    {
        Vector2[] _vertices = new Vector2[6];
        // To decrease memory consumption it is possible to remove neighbor list and infere neighbours via edges.
        TCell[] _neighbors = new TCell[6];
        TEdge[] _edges = new TEdge[6];

        public Vector2? Center { get; set; }
        public void AddNeighbor(TCell cell, int direction) => _neighbors[direction] = cell;
        public void AddVertex(Vector2 vertex, int direction) => _vertices[direction] = vertex;
        public void AddEdge(TEdge edge, int direction) => _edges[direction] = edge;
        public TCell GetNeighbor(int direction) => _neighbors[direction % 6];
        public Vector2 GetVertex(int direction) => _vertices[direction % 6];
        public TEdge GetEdge(int direction) => _edges[direction % 6];
        public IEnumerable<Vector2> Vertices => _vertices;
        public IEnumerable<TCell> Neighbors => _neighbors.Where(n => n != null);
        public IEnumerable<TEdge> Edges => _edges;
        public TEdge? GetEdgeByNeighbor(TCell neighbor) => 
            neighbor == null ? default :
            _edges.Select((e, i) => (e, i)).First(t => neighbor.Equals(_neighbors[t.i])).e;
        public int GetDirection(TEdge edge) => _edges.ToList().IndexOf(edge);
        public int GridPositionX { get; set; }
        public int GridPositionY { get; set; }
    }

    /// <summary>
    /// Concrete representation of the generic HexCell class.
    /// </summary>
    public class HexCell : HexCell<HexCell, Edge>
    {

    }
}
