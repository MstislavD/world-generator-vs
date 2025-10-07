using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldSimulationForm
{
    class SmoothHeightRender
    {
        public static Bitmap Draw(int width, int height)
        {
            Bitmap image = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(image);
            g.Clear(Color.White);

            float radius = height / 2f;
            PointF origin = new PointF(width / 2, radius);

            PointF[] triangle = _polygonPoints(3, radius/2, origin);
            Color[] triangleColors = new Color[] { Color.Red, Color.Green, Color.Blue };

            PointF[] square = _polygonPoints(4, radius, origin);

            PointF[] septagon = _polygonPoints(7, radius, origin);
            Color[] septagonColors = new Color[] { Color.DarkViolet, Color.Blue, Color.Aqua, Color.LimeGreen, Color.Yellow, Color.Orange, Color.Red };

            RandomExtension.RandomExt random = new RandomExtension.RandomExt();
            int sides = random.Next(3, 9);
            PointF[] polygonPoints = _polygonPoints(sides, radius, origin);
            Color[] colors = Enumerable.Range(0, sides).Select(i => random.NextVector(3, 256)).Select(v => Color.FromArgb(v[0], v[1], v[2])).ToArray();

            //DrawPath(g, triangle, triangleColors);
            DrawPath(g, square, triangle, triangleColors);

            g.DrawPolygon(Pens.Black, square);

            g.DrawCurve(Pens.Black, square);


            //PointF v0 = triangle[0];
            //PointF v1 = triangle[1];
            //PointF v2 = triangle.Last();

            //float dist = (float)Math.Sqrt((v0.X - v1.X) * (v0.X - v1.X) + (v0.Y - v1.Y) * (v0.Y - v1.Y));
            //float delta = dist / 100000;


            //for (int i=0; i<10; i++)
            //{
            //    g.DrawPolygon(Pens.Black, triangle);

            //    _repairPoint(triangle, 0, v1, v2, delta);
            //    _repairPoint(triangle, 1, v0, v2, delta);
            //    _repairPoint(triangle, triangle.Length - 1, v0, v1, delta);
            //}

            return image;
        }

        public static void FillPath(Graphics g, PointF[] polygonPoints, Color[] polygonColors)
        {
            //PointF[] repaired = polygonPoints.ToArray();

            //PointF v0 = repaired[0];
            //PointF v1 = repaired[1];
            //PointF v2 = repaired.Last();

            //float dist = (float)Math.Sqrt((v0.X - v1.X) * (v0.X - v1.X) + (v0.Y - v1.Y) * (v0.Y - v1.Y));
            //float delta = dist * 0.001f;

            //_repairPoint(repaired, 0, v1, v2, delta);
            //_repairPoint(repaired, 1, v0, v2, delta);
            //_repairPoint(repaired, repaired.Length - 1, v0, v1, delta);

            GraphicsPath path = new GraphicsPath();
            path.AddPolygon(polygonPoints);           

            PathGradientBrush brush = new PathGradientBrush(path);
            brush.SurroundColors = polygonColors;
            
            //brush.CenterColor = blend(brush.SurroundColors);
            brush.CenterColor = polygonColors[0];
            brush.CenterPoint = polygonPoints[0];

            g.FillPolygon(brush, polygonPoints);

            brush.Dispose();

        }

        public static void DrawPath(Graphics g, PointF[] polygonPoints, Color[] polygonColors)
        {
            PointF[] expandedPath = polygonPoints.ToArray();

            PointF v0 = expandedPath[0];
            PointF v1 = expandedPath[1];
            PointF v2 = expandedPath.Last();

            float dist = (float)Math.Sqrt((v0.X - v1.X) * (v0.X - v1.X) + (v0.Y - v1.Y) * (v0.Y - v1.Y));
            float delta = dist * 0.01f;

            _repairPoint(expandedPath, 0, v1, v2, delta);
            _repairPoint(expandedPath, 1, v0, v2, delta);
            _repairPoint(expandedPath, expandedPath.Length - 1, v0, v1, delta);

            GraphicsPath path = new GraphicsPath();
            path.AddPolygon(expandedPath);

            PathGradientBrush brush = new PathGradientBrush(path);
            brush.SurroundColors = polygonColors;
            brush.CenterColor = blend(brush.SurroundColors);

            Pen pen = new Pen(brush, 5);

            g.DrawPolygon(pen, polygonPoints);

            brush.Dispose();

        }

        public static void DrawPath(Graphics g, PointF[] polygonPoints, PointF[] brushPoints, Color[] polygonColors)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddPolygon(brushPoints);
            //path.AddCurve(brushPoints);

            //path.AddClosedCurve(brushPoints);

            

            PathGradientBrush brush = new PathGradientBrush(path);
            brush.SurroundColors = polygonColors;
            brush.CenterColor = blend(brush.SurroundColors);

            brush.WrapMode = WrapMode.Tile;

            Region region = new Region(path);

            g.FillPolygon(brush, polygonPoints);
            g.FillRegion(brush, region);


            brush.Dispose();

        }

        private static void _repairPoint(PointF[] repaired, int index, PointF v1, PointF v2, float delta)
        {
            PointF v0 = repaired[index];

            PointF vv1 = new PointF(v0.X - v1.X, v0.Y - v1.Y);
            PointF vv2 = new PointF(v0.X - v2.X, v0.Y - v2.Y);
            repaired[index] = new PointF(v0.X + (vv1.X + vv2.X) * delta, v0.Y + (vv1.Y + vv2.Y) * delta);
        }

        static PointF[] _polygonPoints(int sides, float radius, PointF origin)
        {
            PointF[] points = new PointF[sides];

            float angle = -(float)Math.PI/2;
            float angleStep = (float)Math.PI * 2 / sides;

            for (int i = 0; i < sides; i++)
            {
                float x = origin.X + (float)Math.Cos(angle) * radius;
                float y = origin.Y + (float)Math.Sin(angle) * radius;
                points[i] = new PointF(x, y);
                angle += angleStep;
            }

            return points;
        }

        static Color blend(Color[] colors)
        {
            int red = 0;
            int green = 0;
            int blue = 0;
            for (int i = 0; i < colors.Length; i++)
            {
                red += colors[i].R;
                green += colors[i].G;
                blue += colors[i].B;
            }

            return Color.FromArgb(red / colors.Length, green / colors.Length, blue / colors.Length);
        }
    }
}
