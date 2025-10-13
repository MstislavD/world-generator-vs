using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topology;

namespace WorldSimulationForm
{
    internal class RaycastTest
    {
        static internal Bitmap GetImage(int size)
        {
            Random r = new Random();

            int points = 100000;
            int pointSize = int.Clamp((int)MathF.Sqrt(size * size / points * 0.5f), 2, 10);
            int pr = pointSize / 2;

            IPolygon polygon = getStar(size);

            Stopwatch sw = Stopwatch.StartNew();

            Dictionary<Vector2, bool> insideByPoint = new Dictionary<Vector2, bool>();
            for (int i = 0; i < points; i++)
            {
                Vector2 point = new Vector2(r.Next(size), r.Next(size));
                insideByPoint[point] = polygon.ContainsPoint(point);
            }

            Debug.WriteLine($"{points} raycasts for polygon with {polygon.VertexCount} vertices in {sw.ElapsedMilliseconds} ms");

            Bitmap image = new Bitmap(size, size);

            Graphics g = Graphics.FromImage(image);
            g.Clear(Color.White);

            foreach(KeyValuePair<Vector2, bool> p in insideByPoint)
            {
                Point point = new Point((int)p.Key.X - pr, (int)p.Key.Y - pr);
                g.FillEllipse(insideByPoint[p.Key] ? Brushes.Red : Brushes.Blue, new(point, new Size(pointSize, pointSize)));
            }

            g.DrawLines(Pens.Black, polygon.Vertices.Append(polygon.Vertices.First()).Select(v => new PointF((float)v.X, (float)v.Y)).ToArray());

            return image;
        }

        static IPolygon getHexagon(int size)
        {
            HexCell hex = new HexCell();
            double outerR = size * 0.4;
            double innerR = outerR * Math.Sqrt(3);

            hex.Center = 0.5 * new Vector2(size, size);

            Vector2[] vertices = [
                new Vector2(hex.Center.X, hex.Center.Y - outerR),
                new Vector2(hex.Center.X + 0.5 * innerR, hex.Center.Y - outerR / 2),
                new Vector2(hex.Center.X + 0.5 * innerR, hex.Center.Y + outerR / 2),
                new Vector2(hex.Center.X, hex.Center.Y + outerR),
                new Vector2(hex.Center.X - 0.5 * innerR, hex.Center.Y + outerR / 2),
                new Vector2(hex.Center.X - 0.5 * innerR, hex.Center.Y - outerR / 2)
                ];

            for (int i = 0; i < 6; i++)
            {
                hex.SetVertex(vertices[i], i);
            }

            return hex;
        }

        static IPolygon getStar(int size, int points = 10)
        {
            int r1 = (int)(size * 0.2f);
            int r2 = (int)(size * 0.4f);
            List<Vector2> vertices = new List<Vector2>();
            Vector2 center = new Vector2(size / 2, size / 2);

            for (int i = 0; i < points; i++)
            {
                int dx = -(int)(r2 * Math.Sin(Math.PI * 2 / points * i));
                int dy = -(int)(r2 * Math.Cos(Math.PI * 2 / points * i));
                vertices.Add(new Vector2(center.X + dx, center.Y + dy));

                int dx2 = -(int)(r1 * Math.Sin(Math.PI * 2 / points * (i + 0.5)));
                int dy2 = -(int)(r1 * Math.Cos(Math.PI * 2 / points * (i + 0.5)));
                vertices.Add(new Vector2(center.X + dx2, center.Y + dy2));
            }

            return new Polygon(vertices);
        }

        class Polygon : IPolygon
        {
            public BoundingBox? _bbox { get; set; } = null;

            List<Vector2> _vertices = new List<Vector2>();

            public IEnumerable<Vector2> Vertices => _vertices;

            public int VertexCount => _vertices.Count();

            public Polygon(IEnumerable<Vector2> vertices)
            {
                _vertices.AddRange(vertices);
            }
        }
    }
}
