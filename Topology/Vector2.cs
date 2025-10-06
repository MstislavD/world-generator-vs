namespace Topology
{
    /// <summary>
    /// Representation of a 2-dimensional vector.
    /// </summary>
    /// <remarks>
    /// <para>Analogous to System.Numerics.Vector2 struct, but is used as a reference type to
    /// decrease memory consumption (one vector is shared by three hexagons).</para>
    /// <para>Should use float values but PointLocation algorithm currently doesn't converge when dealing with 
    /// floats for some reason.</para>
    /// </remarks>
    ///
    public class Vector2
    {
        public static Vector2 Lerp(Vector2 v1, Vector2 v2, double a) => new Vector2(double.Lerp(v1.X, v2.X, a), double.Lerp(v1.Y, v2.Y, a));
        public static double DistanceSquared(Vector2 v1, Vector2 v2) => (v1.X - v2.X) * (v1.X - v2.X) + (v1.Y - v2.Y) * (v1.Y - v2.Y);
        public static double Distance(Vector2 v1, Vector2 v2) => Math.Sqrt(DistanceSquared(v1, v2));
        public static Vector2 operator +(Vector2 v1, Vector2 v2) => new(v1.X + v2.X, v1.Y + v2.Y);
        public static Vector2 operator -(Vector2 v1, Vector2 v2) => new(v1.X - v2.X, v1.Y - v2.Y);
        public static Vector2 operator -(Vector2 v) => new(-v.X, -v.Y);
        public static Vector2 operator *(double a, Vector2 v) => new(a * v.X, a * v.Y);
        public Vector2(double x, double y) {X = x; Y = y;}
        public Vector2(Vector2 v) {X = v.X; Y = v.Y;}
        public double X { get; internal set; }
        public double Y { get; internal set; }
        public override string ToString() => $"[{X}, {Y}]";
    }
}
