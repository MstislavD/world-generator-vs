using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Topology
{
    public static class TempClass
    {
        public static Vector2 Between(Vector2 v1, Vector2 v2, float t)
        {
            return v1 + t * (v2 - v1);
        }

        public static Vector2 Crossing(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 v4)
        {
            double epsilon = 1e-10;
            if (Math.Abs(v1.X - v2.X) < epsilon)
            {
                float x = v1.X;
                float a2 = (v3.Y - v4.Y) / (v3.X - v4.X);
                float b2 = v3.Y - a2 * v3.X;
                float y = a2 * x + b2;
                return new(x, y);
            }
            else if (Math.Abs(v3.X - v4.X) < epsilon)
            {
                float x = v3.X;
                float a1 = (v1.Y - v2.Y) / (v1.X - v2.X);
                float b1 = v1.Y - a1 * v1.X;
                float y = a1 * x + b1;
                return new(x, y);
            }
            else
            {
                float a1 = (v1.Y - v2.Y) / (v1.X - v2.X);
                float b1 = v1.Y - a1 * v1.X;
                float a2 = (v3.Y - v4.Y) / (v3.X - v4.X);
                float b2 = v3.Y - a2 * v3.X;
                float x = (b2 - b1) / (a1 - a2);
                float y = a1 * x + b1;
                return new(x, y);
            }
        }
    }
}
