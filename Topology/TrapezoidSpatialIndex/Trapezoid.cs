using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topology;

namespace TrapezoidSpatialIndex
{
    public class PLNode
    {
        public Vector2 Vertex { get; set; }
        public LineSegment Edge { get; set; }
        public Trapezoid Trapezoid { get; set; }
        public PLNode Left { get; set; }
        public PLNode Right { get; set; }
    }

    public class Trapezoid
    {
        public LineSegment Top { get; set; }
        public LineSegment Bottom { get; set; }
        public Vector2 Left { get; set; }
        public Vector2 Right { get; set; }
        public Trapezoid UpperLeft { get; set; }
        public Trapezoid LowerLeft { get; set; }
        public Trapezoid UpperRight { get; set; }
        public Trapezoid LowerRight { get; set; }
        public PLNode Node { get; set; }
        public IEnumerable<Trapezoid> Neighbors(Func<Trapezoid, bool> include)
        {
            if (include(UpperLeft))
                yield return UpperLeft;
            if (include(LowerLeft))
                yield return LowerLeft;
            if (include(UpperRight))
                yield return UpperRight;
            if (include(LowerRight))
                yield return LowerRight;
        }

        public override string ToString()
        {
            return $"L: {Left}, R: {Right}, Top: {Top}, Bottom: {Bottom}";
        }
    }
}
