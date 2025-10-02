using System.Collections.Concurrent;
using System.Collections.Generic;
using Topology;

namespace WorldSimulator
{
    class RenderObjects
    {
        public double Multiplier;
        public Vector2 Origin;
        public double Scale;
        public List<ImageData> Images { get; } = new List<ImageData>();
        public List<SegmentData> Segments { get; } = new List<SegmentData>();
        public List<PolygonData> Polygons { get; } = new List<PolygonData>();
        public List<SegmentData> PreImageSegments { get; } = new List<SegmentData>();
        public List<VertexData> Vertices { get; } = new List<VertexData>();
    }
}
