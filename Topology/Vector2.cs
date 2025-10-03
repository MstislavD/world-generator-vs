namespace Topology
{
    /// <summary>
    /// Representation of a 2-dimensional vector. Analogous to System.Numerics.Vector2 struct.
    /// </summary>
    /// <remarks>Ideally should use System.Numerics.Vector2 instead, but currently it is not possible, because 
    /// WorldSimulation algorithms use it as a reference type. Also PointLocation algorithm is running indefenitely when dealing with 
    /// float values for some reason.</remarks>
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
        public double X { get; set; }
        public double Y { get; set; }
        public override string ToString() => $"[{X}, {Y}]";
    }
}
