using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexGrid
{
    public class Edge
    {
        public HexCell Cell1 { get; set; }
        public HexCell Cell2 { get; set; }
        public Vertex Vertex1 { get; set; }
        public Vertex Vertex2 { get; set; }
        public IEnumerable<HexCell> Cells => new List<HexCell>() { Cell1, Cell2 }.Where(c => c != null);
        public Vertex Center => Vertex.Between(Vertex1, Vertex2, 0.5);
        public Vertex Left => Vertex1.X < Vertex2.X ? Vertex1 : Vertex2;
        public Vertex Right => Vertex1.X <= Vertex2.X ? Vertex2 : Vertex1;

        public Vertex GetIntersectionByX(double x)
        {
            double a = (Vertex1.Y - Vertex2.Y) / (Vertex1.X - Vertex2.X);
            double b = Vertex1.Y - a * Vertex1.X;
            double y = a * x + b;

            return new Vertex(x, y);
        }

        public override string ToString()
        {
            return $"{Left} - {Right}";
        }
    }
}
