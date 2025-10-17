using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorldSimulation;
using Topology;
using Utilities;
using System.Diagnostics;

namespace WorldSimulationForm.Tests
{
    class TestLayerHex : Topology.LayerHexCell<TestLayerHex, TestLayerEdge> { }
    class TestLayerEdge : LayerEdge<TestLayerHex, TestLayerEdge> { }
    class TestLayerGrid : HexGrid<TestLayerHex, TestLayerEdge>
    {
        public TestLayerGrid(int columns, int rows) : base(columns, rows) { }
    }

    class Factory : Topology.IFactoryHexGrid<TestLayerGrid>
    {
        public TestLayerGrid CreateGrid(int columns, int rows) => new TestLayerGrid(columns, rows);
    }

    internal class LayerGridTest
    {
        public static Bitmap? GetImage(Size maxSize)
        {
            RandomExt rng = new RandomExt();

            TestLayerGrid parentGrid = new TestLayerGrid(50, 35);

            Dictionary<TestLayerHex, SolidBrush> colorByParent = 
                parentGrid.Cells.Zip(
                    parentGrid.Cells.Select(c => Color.FromArgb(rng.Next())).Select(c => new SolidBrush(Color.FromArgb(255, c)))).ToDictionary();

            int? sv = 0;
            TestLayerGrid childGrid = ChildGridGenerator.CreateChildGrid<TestLayerGrid, TestLayerHex, TestLayerEdge>(parentGrid, new Factory(), rng, sizeVariance: sv);

            double avg = (double)childGrid.CellCount / parentGrid.CellCount;

            double std = parentGrid.Cells.Select(c => c.ChildrenCount - avg).Average(d => Math.Sqrt(Math.Pow(d, 2)));
            int min = parentGrid.Cells.Min(c => c.ChildrenCount);
            int max = parentGrid.Cells.Max(c => c.ChildrenCount);

            var counts = parentGrid.Cells.CountBy(c => c.ChildrenCount).ToDictionary();

            Debug.WriteLine($"Std = {std:F2}, Max = {max}");
            Debug.WriteLine(string.Concat(counts.Keys.Order().Select(i => $"{i}: {counts[i]}, ")));

            var objects = new RenderObjects();
            objects.Polygons.AddRange(childGrid.Cells.Select(c => new PolygonData(c.Vertices, colorByParent[c.Parent])));
            objects.Segments.AddRange(childGrid.Edges.Where(e => e.Parent != null).Select(e => new SegmentData([e.Vertex1, e.Vertex2], Pens.Black)));

            Bitmap image = HexGridRenderer.Render(childGrid, maxSize.Width, maxSize.Height, objects);

            return image;            
        }
    }
}
