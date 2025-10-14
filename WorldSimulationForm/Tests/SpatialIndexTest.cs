using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Topology;
using WorldSimulation;

namespace WorldSimulationForm.Tests
{
    internal class SpatialIndexTest
    {
        public static Bitmap? GetImage(WorldGenerator generator, Size maxSize)
        {
            SubregionGraph graph = generator.SubregionGraph;

            if (graph == null)
                return null;

            HexGrid grid = generator.GetGrid(3);

            var objects = new RenderObjects();

            foreach (var edge in graph.Subregions.SelectMany(s => s.Edges))
            {
                objects.PreImageSegments.Add(new SegmentData(edge.Vertices, Pens.Black));
            }

            BoundingBox bbox = new BoundingBox(-graph.Width * 0.25, -graph.Height * 0.25, graph.Width * 1.25, graph.Height * 1.25);

            Stopwatch sw = Stopwatch.StartNew();

            PolygonSpatialIndex<Subregion> index = new PolygonSpatialIndex<Subregion>(graph.Subregions, bbox, 5);

            Debug.WriteLine($"Spatial index calculated in {sw.ElapsedMilliseconds} ms");
            sw.Restart();

            int columns = 1600;
            int rows = (int)(graph.Height / graph.Width * columns);
            double side = graph.Width / columns;

            Dictionary<Subregion, List<Vector2>> pointsBySubregion = graph.Subregions.ToDictionary(s => s, s => new List<Vector2>());

            Action<int> doColumn = i =>
            {
                for (int j = 0; j < rows; j++)
                {
                    Vector2 center = new Vector2((i + 0.5) * side, (j + 0.5) * side);

                    Subregion? subregion =
                        index.FindPolygonContainingPoint(center) ??
                        index.FindPolygonContainingPoint(new Vector2(center.X - graph.Width, center.Y)) ??
                        index.FindPolygonContainingPoint(new Vector2(center.X + graph.Width, center.Y));

                    if (subregion != null)
                        lock (pointsBySubregion)
                            pointsBySubregion[subregion].Add(center);
                }
            };

            Parallel.For(0, columns, doColumn);

            double msPerIndex = Math.Round((double)sw.ElapsedMilliseconds / (columns * rows), 4);
            Debug.WriteLine($"{columns} x {rows} ({columns * rows}) points indexed in {sw.ElapsedMilliseconds} ms ({msPerIndex} ms per point)");
            sw.Restart();

            Bitmap overlay = HexGridRenderer.Render(grid, maxSize.Width, maxSize.Height, objects);
            overlay.MakeTransparent(Color.White);

            Bitmap image = new Bitmap(overlay.Width, overlay.Height);
            Graphics g = Graphics.FromImage(image);

            // randomize subregion colors
            Random rnd = new Random();
            Dictionary<Subregion, Brush> brushBySubregion = new Dictionary<Subregion, Brush>();
            Dictionary<Subregion, Pen> penBySubregion = new Dictionary<Subregion, Pen>();
            foreach (Subregion s in graph.Subregions)
            {
                Color color = Color.FromArgb(rnd.Next());
                color = Color.FromArgb(255, color);
                brushBySubregion[s] = new SolidBrush(color);
                penBySubregion[s] = new Pen(color);
            }

            int rectSide = overlay.Width / columns;
            int rh = rectSide / 2;
            double scale = overlay.Width / graph.Width;
            GraphicsPath path = new GraphicsPath();
            foreach (var s in pointsBySubregion)
            {                             
                Rectangle[] rects = s.Value.Select(p => new Rectangle((int)(p.X * scale - rh), (int)(p.Y * scale - rh), rectSide, rectSide)).ToArray();
                if (rects.Length > 0)
                {
                    g.FillRectangles(brushBySubregion[s.Key], rects);
                    g.DrawRectangles(penBySubregion[s.Key], rects);
                }
            }

            g.DrawImage(overlay, 0, 0);

            Debug.WriteLine($"Image generated in {sw.ElapsedMilliseconds} ms");

            return image;
        }
    }
}
