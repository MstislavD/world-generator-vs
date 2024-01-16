using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public abstract class VectorConverter<T>
    {
        public abstract double[] ObjectToVector(T item);
        public abstract T ObjectFromVector(double[] vector);
    }

    public class Interpolation
    {
        static public int Interpolate(Dictionary<double, int> points, double value)
        {
            double v1 = points.First().Key;
            double v2;
            int item1 = points.First().Value;
            int item2;
            foreach (KeyValuePair<double, int> pair in points)
            {
                item2 = pair.Value;
                v2 = pair.Key;
                if (value < v1)
                {
                    return pair.Value;
                }
                else if (value < v2)
                {
                    double a = (value - v1) / (v2 - v1);
                    return (int)(a * (item2 - item1) + item1);
                }
                v1 = v2;
                item1 = item2;
            }
            return item1;
        }

        static public double Interpolate(Dictionary<double, double> points, double value)
        {
            double v1 = points.First().Key;
            double v2;
            double item1 = points.First().Value;
            double item2;
            foreach (KeyValuePair<double, double> pair in points)
            {
                item2 = pair.Value;
                v2 = pair.Key;
                if (value < v1)
                {
                    return pair.Value;
                }
                else if (value < v2)
                {
                    double a = (value - v1) / (v2 - v1);
                    return a * (item2 - item1) + item1;
                }
                v1 = v2;
                item1 = item2;
            }
            return item1;
        }

        static public T Interpolate<T>(Dictionary<double, T> points, double value, VectorConverter<T> converter)
        {
            double v1 = points.First().Key;
            double v2;
            T item1 = points.First().Value;
            T item2;
            foreach (KeyValuePair<double, T> pair in points)
            {
                item2 = pair.Value;
                v2 = pair.Key;
                if (value < v1)
                {
                    return pair.Value;
                }
                else if (value < v2)
                {
                    double a = (value - v1) / (v2 - v1);
                    double[] vector1 = converter.ObjectToVector(item1);
                    double[] vector2 = converter.ObjectToVector(item2);
                    double[] vectorR = Enumerable.Range(0, vector1.Length).Select(i => a * (vector2[i] - vector1[i]) + vector1[i]).ToArray();
                    return converter.ObjectFromVector(vectorR);
                }
                v1 = v2;
                item1 = item2;
            }
            return item1;
        }
    }
}
