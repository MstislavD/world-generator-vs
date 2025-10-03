using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topology;
using System.Drawing;

namespace WorldSimulator
{ 
    class HexGridRenderer
    {
        static double ridgeMultiplier = 0.5;
        static float _radius = 5;

        Bitmap _image;
        RenderObjects _objects;

        static public Bitmap Render(HexGrid grid, int maxWidth, int maxHeight, RenderObjects objects)
        {
            return new HexGridRenderer(grid, maxWidth, maxHeight, objects)._image;
        }

        HexGridRenderer(HexGrid grid, int maxWidth, int maxHeight, RenderObjects objects)
        {
            double xScale = (maxWidth - 1) / grid.Width;
            double yScale = (maxHeight - 1) / grid.Height;
            double scale = Math.Min(xScale, yScale);

            int imageWidth = (int)(grid.Width * scale) + 1;
            int imageHeight = (int)(grid.Height * scale) + 1;

            _objects = objects;

            _objects.Multiplier = _objects.Multiplier > 0 ? _objects.Multiplier : 1;

            _image = new Bitmap(imageWidth, imageHeight);

            _objects.Scale = scale;
            if (_objects.Origin == null)
                _objects.Origin = new Vector2(0, 0);

            Graphics g = Graphics.FromImage(_image);

            foreach (PolygonData polygon in _objects.Polygons)
            {
                PointF[] points = polygon.Vertices.Select(v => PointFromVertex(v)).ToArray();
                g.FillPolygon(polygon.Brush, points);

                if (polygon.Vertices.Any(v => v.X > grid.Width))
                {
                    points = polygon.Vertices.Select(v => PointFromVertex(v, -imageWidth + 1)).ToArray();
                    g.FillPolygon(polygon.Brush, points);
                }
                if (polygon.Vertices.Any(v => v.X <= 0))
                {
                    points = polygon.Vertices.Select(v => PointFromVertex(v, imageWidth - 1)).ToArray();
                    g.FillPolygon(polygon.Brush, points);
                }
            }

            foreach (VertexData v in _objects.Vertices)
            {
                PointF point = PointFromVertex(v.Vertex);
                g.FillEllipse(v.Brush, point.X - _radius, point.Y - _radius, _radius * 2, _radius * 2);
            }

            foreach (SegmentData segment in _objects.PreImageSegments)
            {
                _drawSegments(grid, imageWidth, g, segment);
            }

            foreach (ImageData sprite in _objects.Images)
            {
                float width = sprite.Stretch ? (float)(sprite.Image.Width * objects.Multiplier) : sprite.Image.Width * sprite.Scale;
                float height = sprite.Stretch ? (float)(sprite.Image.Height * objects.Multiplier) : sprite.Image.Height * sprite.Scale;

                PointF point = PointFromVertex(sprite.Vertex);
                point.X -= width / 2;
                point.Y -= height / 2;

                g.DrawImage(sprite.Image, point.X, point.Y, width, height);
            }

            foreach (SegmentData segment in objects.Segments)
            {
                _drawSegments(grid, imageWidth, g, segment);
            }
        }

        private void _drawSegments(HexGrid grid, int imageWidth, Graphics g, SegmentData segment)
        {
            if (segment.Pen.Width == 0)
                segment.Pen.Width = (int)(grid.HexSide * ridgeMultiplier * _objects.Scale);

            PointF[] points = segment.Vertices.Select(v => PointFromVertex(v)).ToArray();
            points = _repair(points, imageWidth);
            g.DrawLines(segment.Pen, points);

            if (segment.Vertices.Any(v => v.X > grid.Width))
            {
                points = segment.Vertices.Select(v => PointFromVertex(v, -imageWidth + 1)).ToArray();
                g.DrawLines(segment.Pen, points);
            }
            if (segment.Vertices.Any(v => v.X <= 0))
            {
                points = segment.Vertices.Select(v => PointFromVertex(v, imageWidth - 1)).ToArray();
                g.DrawLines(segment.Pen, points);
            }
        }

        PointF PointFromVertex(Vector2 vertex)
        {
            float x = (float)((vertex.X - _objects.Origin.X) * _objects.Scale * _objects.Multiplier);
            float y = (float)((vertex.Y - _objects.Origin.Y) * _objects.Scale * _objects.Multiplier);
            return new PointF(x, y);
        }            

        PointF PointFromVertex(Vector2 vertex, double shift)
        {
            float x = (float)(((vertex.X - _objects.Origin.X) * _objects.Scale + shift) * _objects.Multiplier);
            float y = (float)((vertex.Y - _objects.Origin.Y) * _objects.Scale * _objects.Multiplier);
            return new PointF(x, y);
        }

        static PointF[] _repair(PointF[] points, float width)
        {
            PointF reference = points[0];
            List<PointF> repaired = new List<PointF>() { reference };
            foreach(PointF point in points.Skip(1))
            {
                float x = point.X;
                if (reference.X - x > width / 2)
                    x += width;
                else if (x - reference.X > width / 2)
                    x -= width;
                repaired.Add(new PointF(x, point.Y));
            }
            return repaired.ToArray();
        }
            

    }
}
