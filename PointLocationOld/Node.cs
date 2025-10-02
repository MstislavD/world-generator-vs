using HexGrid;

namespace PointLocation
{
    public class Node
    {
        public Vertex Vertex { get; set; }
        public Edge Edge { get; set; }
        public Trapezoid Trapezoid { get; set; }
        public Node Left { get; set; }
        public Node Right { get; set; }
    }
}
