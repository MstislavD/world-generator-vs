using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topology
{
    public class Vector2
    {
        public static Vector2 Crossing(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 v4)
        {
            double epsilon = 1e-10;
            if (Math.Abs(v1.X - v2.X) < epsilon)
            {
                double x = v1.X;
                double a2 = (v3.Y - v4.Y) / (v3.X - v4.X);
                double b2 = v3.Y - a2 * v3.X;
                double y = a2 * x + b2;
                return new Vector2(x, y);
            }
            else if (Math.Abs(v3.X - v4.X) < epsilon)
            {
                double x = v3.X;
                double a1 = (v1.Y - v2.Y) / (v1.X - v2.X);
                double b1 = v1.Y - a1 * v1.X;
                double y = a1 * x + b1;
                return new Vector2(x, y);
            }
            else
            {
                double a1 = (v1.Y - v2.Y) / (v1.X - v2.X);
                double b1 = v1.Y - a1 * v1.X;
                double a2 = (v3.Y - v4.Y) / (v3.X - v4.X);
                double b2 = v3.Y - a2 * v3.X;
                double x = (b2 - b1) / (a1 - a2);
                double y = a1 * x + b1;
                return new Vector2(x, y);
            }
        }

        public static Vector2 Between(Vector2 v1, Vector2 v2, double a)
        {
            double x = v1.X + a * (v2.X - v1.X);
            double y = v1.Y + a * (v2.Y - v1.Y);
            return new Vector2(x, y);
        }

        public static double DistanceSquared(Vector2 v1, Vector2 v2)
        {
            return (v1.X - v2.X) * (v1.X - v2.X) + (v1.Y - v2.Y) * (v1.Y - v2.Y);
        }

        public static double Distance(Vector2 v1, Vector2 v2)
        {
            return Math.Sqrt((v1.X - v2.X) * (v1.X - v2.X) + (v1.Y - v2.Y) * (v1.Y - v2.Y));
        }

        public static Vector2 operator +(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.X + v2.X, v1.Y + v2.Y);
        }

        public static Vector2 operator -(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.X - v2.X, v1.Y - v2.Y);
        }

        public static Vector2 operator -(Vector2 v)
        {
            return new Vector2(-v.X, -v.Y);
        }

        public static Vector2 operator *(double a, Vector2 v)
        {
            return new Vector2(a * v.X, a * v.Y);
        }

        public static Vector2 operator *(Vector2 v, double a)
        {
            return new Vector2(a * v.X, a * v.Y);
        }

        public static Vector2 operator *(int a, Vector2 v)
        {
            return new Vector2(a * v.X, a * v.Y);
        }

        public static Vector2 operator *(Vector2 v, int a)
        {
            return new Vector2(a * v.X, a * v.Y);
        }

        public static Vector2 operator *(float a, Vector2 v)
        {
            return new Vector2(a * v.X, a * v.Y);
        }

        public static Vector2 operator *(Vector2 v, float a)
        {
            return new Vector2(a * v.X, a * v.Y);
        }


        public Vector2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public Vector2(Vector2 v)
        {
            X = v.X;
            Y = v.Y;
        }

        public double X { get; set; }
        public double Y { get; set; }

        public override string ToString()
        {
            return $"[{X}, {Y}]";
        }
    }
}
