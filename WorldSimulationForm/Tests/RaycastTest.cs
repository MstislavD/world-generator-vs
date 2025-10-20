using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topology;

namespace WorldSimulationForm.Tests
{
    internal class RaycastTest
    {
        static internal Bitmap GetImage(int size)
        {
            Random r = new Random();

            int points = 100000;
            int pointSize = int.Clamp((int)MathF.Sqrt(size * size / points * 0.5f), 2, 10);
            int pr = pointSize / 2;

            IPolygon polygon = Polygon.GetStar(size, points: 50);

            Stopwatch sw = Stopwatch.StartNew();

            Dictionary<Vector2, bool> insideByPoint = new Dictionary<Vector2, bool>();
            for (int i = 0; i < points; i++)
            {
                Vector2 point = new Vector2(r.Next(size), r.Next(size));
                insideByPoint[point] = Polygon.ContainsPoint(polygon, point);
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
    }
}
