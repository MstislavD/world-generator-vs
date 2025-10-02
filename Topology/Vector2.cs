namespace Topology
{
    /// <summary>
    /// Representation of a 2-dimensional vector
    /// </summary>
    /// <remarks>Should use Vector2 struct from System.Numerics instead, but currently it is not possible, because 
    /// WorldSimulation algorithms use it as a reference type. Also PointLocation algorithm is running indefenitely when dealing with 
    /// float values for some reason.</remarks>
    public class Vector2
    {
        /// <summary>
        /// Finds the intersection point of two lines, each defined by two points
        /// </summary>
        /// <remarks> Probably belongs elsewhere</remarks>
        /// <param name="line1_p1">First point of the first line.</param>
        /// <param name="line1_p2">Second point of the first line.</param>
        /// <param name="line2_p1">First point of the second line.</param>
        /// <param name="line2_p2">Second point of the second line.</param>
        /// <returns>
        /// The intersection point if lines intersect, 
        /// null if lines are parallel or coincident
        /// </returns>
        public static Vector2? FindLineIntersection(Vector2 line1_p1, Vector2 line1_p2, Vector2 line2_p1, Vector2 line2_p2)
        {
            // Calculate the direction vectors
            double dx1 = line1_p2.X - line1_p1.X;
            double dy1 = line1_p2.Y - line1_p1.Y;
            double dx2 = line2_p2.X - line2_p1.X;
            double dy2 = line2_p2.Y - line2_p1.Y;

            // Calculate the determinant
            double determinant = dx1 * dy2 - dx2 * dy1;

            // Check if lines are parallel (determinant is close to zero)
            if (Math.Abs(determinant) < 1e-10)
            {
                return null; // Lines are parallel or coincident
            }

            // Calculate parameters for the intersection point
            double t = ((line2_p1.X - line1_p1.X) * dy2 - (line2_p1.Y - line1_p1.Y) * dx2) / determinant;

            // Calculate the intersection point
            double intersectionX = line1_p1.X + t * dx1;
            double intersectionY = line1_p1.Y + t * dy1;

            return new Vector2(intersectionX, intersectionY);
        }

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
