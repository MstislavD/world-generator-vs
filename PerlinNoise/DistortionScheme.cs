using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perlin
{
    public class DistortionScheme
    {
        Dictionary<int, PerlinNoise> _xNoiseByFrequency;
        Dictionary<int, PerlinNoise> _yNoiseByFrequency;

        public DistortionScheme(Random random, int[] frequencies)
        {
            _xNoiseByFrequency = new Dictionary<int, PerlinNoise>();
            _yNoiseByFrequency = new Dictionary<int, PerlinNoise>();

            PerlinNoise.Settings settings = new PerlinNoise.Settings();

            foreach (int frequency in frequencies)
            {
                _xNoiseByFrequency[frequency] = new PerlinNoise(frequency, frequency, settings, random);
                _yNoiseByFrequency[frequency] = new PerlinNoise(frequency, frequency, settings, random);
            }
        }

        public Point[] DistortPoints(Point[] points, Dictionary<int, double> _distortionByFrequency)
        {
            int count = 0;
            Point[] distortedPoints = new Point[points.Length];
            foreach (Point point in points)
            {
                double distX = 0;
                double distY = 0;

                foreach (int frequency in _distortionByFrequency.Keys)
                {
                    distX += (float)(_distortionByFrequency[frequency] * (_xNoiseByFrequency[frequency].Sample(point.X, point.Y) - 0.5));
                    distY += (float)(_distortionByFrequency[frequency] * (_yNoiseByFrequency[frequency].Sample(point.X, point.Y) - 0.5));
                }
                distortedPoints[count++] = new Point(point.X + distX, point.Y + distY);
            }

            return distortedPoints;
        }
    }
}
