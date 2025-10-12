using Topology;

namespace PointLocation
{
    public class Node
    {
        public Vector2 Vertex { get; set; }
        public LineSegment Edge { get; set; }
        public Trapezoid Trapezoid { get; set; }
        public Node Left { get; set; }
        public Node Right { get; set; }
    }
}
