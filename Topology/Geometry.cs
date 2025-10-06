using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topology
{
    /// <summary>
    /// Collection of various geometric algoritms not strictly connected to any type.
    /// </summary>
    public static class Geometry
    {
        /// <summary>
        /// Finds the intersection point of two lines, each defined by two points.
        /// </summary>
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

        /// <summary>
        /// Returns graph nodes connected to the starting one and satisfying the condition
        /// </summary>
        /// <typeparam name="TCell"></typeparam>
        /// <param name="start"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public static IEnumerable<TCell> Flood<TCell>(TCell start, Func<TCell, bool> condition)
            where TCell : INeighbors<TCell>
        {
            HashSet<TCell> flooded = [];
            HashSet<TCell> flooding = [start];

            while (flooding.Count > 0)
            {
                flooded.UnionWith(flooding);
                flooding = flooding.SelectMany(c => c.Neighbors).Where(condition).Except(flooded).ToHashSet();
            }

            return flooded;
        }

        /// <summary>
        /// Returns true if the graph node is a bottle-neck connection for its neighbors based on
        /// a given property.
        /// </summary>
        /// <typeparam name="TCell"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="node">The node tested.</param>
        /// <param name="value_func">The function that returns a node's property.</param>
        /// <returns></returns>
        public static bool IsConnection<TCell, T>(TCell node, Func<TCell, T> value_func)
            where TCell: INeighbors<TCell>
            where T: notnull
        {
            HashSet<TCell> sameNeighbors = node.Neighbors.Where(c => value_func(c).Equals(value_func(node))).ToHashSet();
            if (sameNeighbors.Count == 0)
            {
                return false;
            }
            TCell starterCell = sameNeighbors.First();
            IEnumerable<TCell> connectedNeighbors = Flood(starterCell, sameNeighbors.Contains);
            return sameNeighbors.Count > connectedNeighbors.Count();
        }
    }
}
