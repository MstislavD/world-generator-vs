using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Perlin
{
    public class ComplexPerlin : PerlinNoise
    {
        List<PerlinNoise> noises;

        public ComplexPerlin(int row, int col, Settings settings, Random random) : base(row, col, settings, random) { }

        public ComplexPerlin(IList<int> frequencies, Settings settings, Random random) : base()
        {
            noises = frequencies.Select(f => new PerlinNoise(f, f, settings, random)).ToList();
        }

        public override float Sample(double x, double y)
        {
            float sample = 1;
            foreach(PerlinNoise noise in noises)
            {
                sample *= noise.Sample(x, y);
            }

            sample = (float)Math.Pow(sample, 1.0 / noises.Count);

            return sample;
        }
    }

    public class PerlinNoise
    {
        public struct Settings
        {
            public bool XStitched;
            public bool YStitched;
        }

        Random _random;
        double[,] _vectors;
        int _xStep;
        int _yStep;
        Settings _settings;

        protected PerlinNoise() { }

        public PerlinNoise(int row, int col, Settings settings, Random random)
        {
            _random = random;
            _xStep = col + 1;
            _yStep = row + 1;
            _settings = settings;
            _vectors = _randomVectors(_xStep, _yStep);
        }

        public override string ToString()
        {
            string info = $"PERLIN NOISE\n";
            for (int j = 0; j < _yStep; j++)
            {
                for (int i = 0; i < _xStep; i++)
                {
                    info += $"{_vectors[i, j]:F3} ({_vectors[i, j] * 360:F1}\u00B0) ";
                }
                info += "\n";
            }
            return info;
        }

        public string NoiseData(int xSize, int ySize)
        {
            string info = $"NOISE DATA FOR {xSize}x{ySize}\n";
            for (int j = 0; j < ySize; j++)
            {
                float y = (j + 0.5f) / xSize;
                for (int i = 0; i < xSize; i++)
                {
                    float x = (i + 0.5f) / ySize;
                    info += $"[{x},{y}]:{_perlin(x, y):F3} ";
                }
                info += "\n";
            }
            return info;
        }

        public float Sample(float x, float y)
        {
            if (_settings.XStitched)
            {
                if (x > 1)
                {
                    x -= 1;
                }
                if (x < 0)
                {
                    x += 1;
                }
            }

            if (x > 1 || x < 0 || y > 1 || y < 0)
            {
                throw new ArgumentException("Both x and y arguments must be in [0,1] range.");
            }

            return (1 + _perlin(x, y)) / 2;
        }

        public virtual float Sample(double x, double y) => Sample((float)x, (float)(y));

        double[,] _randomVectors(int x, int y)
        {
            double[,] vectors = new double[x, y];
            for (int i = 0; i < x; i++)
                for (int j = 0; j < y; j++)
                {
                    vectors[i, j] = _random.NextDouble();
                }

            if (_settings.XStitched)
            {
                for (int j = 0; j < y; j++)
                {
                    vectors[x - 1, j] = vectors[0, j];
                }
            }
            if (_settings.YStitched)
            {
                for (int i = 0; i < x; i++)
                {
                    vectors[i, y - 1] = vectors[i, 0];
                }
            }

            return vectors;
        }

        float _lerp(float a0, float a1, float w) => (1.0f - w) * a0 + w * a1;

        float _dot(int ix, int iy, float x, float y)
        {
            double alpha = _vectors[ix, iy] * 2 * Math.PI;
            return (float)((x - ix) * Math.Cos(alpha) + (y - iy) * Math.Sin(alpha));
        }

        float _perlin(float x, float y)
        {
            Func<float,float> smoothing = t => t * t * t * (t * (t * 6f - 15f) + 10f);
            //smoothing = t => t;

            x = x * (_xStep - 1);
            y = y * (_yStep - 1);

            // Determine grid cell coordinates
            int x0 = (int)(x - (x < (_xStep - 1) ? 0 : 1));
            int y0 = (int)(y - (y < (_yStep - 1) ? 0 : 1));

            // Determine interpolation weights
            // Could also use higher order polynomial/s-curve here
            float sx = smoothing(x - x0);
            float sy = smoothing(y - y0);

            // Interpolate between grid point gradients
            float n0, n1, ix0, ix1;
            n0 = _dot(x0, y0, x, y);
            n1 = _dot(x0 + 1, y0, x, y);
            ix0 = _lerp(n0, n1, sx);
            n0 = _dot(x0, y0 + 1, x, y);
            n1 = _dot(x0 + 1, y0 + 1, x, y);
            ix1 = _lerp(n0, n1, sx);

            return _lerp(ix0, ix1, sy);
        }
    }
}
