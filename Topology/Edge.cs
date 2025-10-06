using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topology
{
    /// <summary>
    /// Representaion of an edge between two nodes of a graph.
    /// </summary>
    /// <typeparam name="TCell">Node of a graph.</typeparam>
    public class Edge<TCell>
    {
        public TCell? Cell1 { get; set; }
        public TCell? Cell2 { get; set; }
        public Vector2 Vertex1 { get; set; }
        public Vector2 Vertex2 { get; set; }
        public IEnumerable<TCell?> Cells => new[] { Cell1, Cell2 }.Where(c => c != null);
        public Vector2 Center => Vector2.Lerp(Vertex1, Vertex2, 0.5);
        public Vector2 Left => Vertex1.X < Vertex2.X ? Vertex1 : Vertex2;
        public Vector2 Right => Vertex1.X <= Vertex2.X ? Vertex2 : Vertex1;

        public Vector2 GetIntersectionByX(double x)
        {
            double t = (x - Vertex2.X) / (Vertex1.X - Vertex2.X);
            return new(x, double.Lerp(Vertex1.Y, Vertex2.Y, t));
        }

        public override string ToString() => $"{Left} - {Right}";
    }

    /// <summary>
    /// Concrete implementation of the generic Edge class.
    /// </summary>
    public class Edge : Edge<HexCell> { }
}
