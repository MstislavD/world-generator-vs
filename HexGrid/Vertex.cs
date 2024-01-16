using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexGrid
{
    public class Vertex
    {
        public static Vertex Crossing(Vertex v1, Vertex v2, Vertex v3, Vertex v4)
        {
            double epsilon = 1e-10;
            if (Math.Abs(v1.X - v2.X) < epsilon)
            {
                double x = v1.X;
                double a2 = (v3.Y - v4.Y) / (v3.X - v4.X);
                double b2 = v3.Y - a2 * v3.X;
                double y = a2 * x + b2;
                return new Vertex(x, y);
            }
            else if (Math.Abs(v3.X - v4.X) < epsilon)
            {
                double x = v3.X;
                double a1 = (v1.Y - v2.Y) / (v1.X - v2.X);
                double b1 = v1.Y - a1 * v1.X;
                double y = a1 * x + b1;
                return new Vertex(x, y);
            }
            else
            {
                double a1 = (v1.Y - v2.Y) / (v1.X - v2.X);
                double b1 = v1.Y - a1 * v1.X;
                double a2 = (v3.Y - v4.Y) / (v3.X - v4.X);
                double b2 = v3.Y - a2 * v3.X;
                double x = (b2 - b1) / (a1 - a2);
                double y = a1 * x + b1;
                return new Vertex(x, y);
            }
        }

        public static Vertex Between(Vertex v1, Vertex v2, double a)
        {
            double x = v1.X + a * (v2.X - v1.X);
            double y = v1.Y + a * (v2.Y - v1.Y);
            return new Vertex(x, y);
        }

        public static double DistanceSquared(Vertex v1, Vertex v2)
        {
            return (v1.X - v2.X) * (v1.X - v2.X) + (v1.Y - v2.Y) * (v1.Y - v2.Y);
        }

        public static double Distance(Vertex v1, Vertex v2)
        {
            return Math.Sqrt((v1.X - v2.X) * (v1.X - v2.X) + (v1.Y - v2.Y) * (v1.Y - v2.Y));
        }

        public static Vertex operator +(Vertex v1, Vertex v2)
        {
            return new Vertex(v1.X + v2.X, v1.Y + v2.Y);
        }

        public static Vertex operator -(Vertex v1, Vertex v2)
        {
            return new Vertex(v1.X - v2.X, v1.Y - v2.Y);
        }

        public static Vertex operator -(Vertex v)
        {
            return new Vertex(-v.X, -v.Y);
        }

        public static Vertex operator *(double a, Vertex v)
        {
            return new Vertex(a * v.X, a * v.Y);
        }

        public static Vertex operator *(Vertex v, double a)
        {
            return new Vertex(a * v.X, a * v.Y);
        }

        public static Vertex operator *(int a, Vertex v)
        {
            return new Vertex(a * v.X, a * v.Y);
        }

        public static Vertex operator *(Vertex v, int a)
        {
            return new Vertex(a * v.X, a * v.Y);
        }

        public static Vertex operator *(float a, Vertex v)
        {
            return new Vertex(a * v.X, a * v.Y);
        }

        public static Vertex operator *(Vertex v, float a)
        {
            return new Vertex(a * v.X, a * v.Y);
        }


        public Vertex(double x, double y)
        {
            X = x;
            Y = y;
        }

        public Vertex(Vertex v)
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
