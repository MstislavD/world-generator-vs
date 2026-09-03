using System;
using System.Collections.Generic;

namespace WorldSimulationForm
{
    /// <summary>
    /// A batch of GDI objects to be rendered in a single pass.
    /// </summary>
    /// <remarks>
    /// <para>Ownership: this instance owns every pen and brush placed into its lists, as well as
    /// everything added to <see cref="Disposables"/> — the caller must dispose it after rendering completes.
    /// Pens/brushes may be shared between entries of a single instance (Dispose is idempotent), but shared
    /// statics such as <c>Brushes.*</c>/<c>Pens.*</c> must never be added to an instance that will be disposed.</para>
    /// <para><see cref="ImageData.Image"/> is NOT owned by this instance: image entries may reference shared
    /// resource bitmaps. Register owned images (e.g. transient copies) in <see cref="Disposables"/> instead.</para>
    /// </remarks>
    class RenderObjects : IDisposable
    {
        public double Multiplier { get; set; } = 1.0;
        public System.Numerics.Vector2? Origin { get; set; }
        public double Scale { get; set; } = 1.0;

        public List<ImageData> Images { get; } = new List<ImageData>();
        public List<PolygonData> Polygons { get; } = new List<PolygonData>();
        public List<SegmentData> Segments { get; } = new List<SegmentData>();
        public List<SegmentData> PreImageSegments { get; } = new List<SegmentData>();
        public List<VertexData> Vertices { get; } = new List<VertexData>();

        /// <summary>Additional owned GDI objects (e.g. transient bitmaps) disposed together with this instance.</summary>
        public List<IDisposable> Disposables { get; } = new List<IDisposable>();

        public void Dispose()
        {
            foreach (PolygonData polygon in Polygons)
                polygon.Brush?.Dispose();

            foreach (VertexData vertex in Vertices)
                vertex.Brush?.Dispose();

            foreach (SegmentData segment in Segments)
                segment.Pen?.Dispose();

            foreach (SegmentData segment in PreImageSegments)
                segment.Pen?.Dispose();

            foreach (IDisposable disposable in Disposables)
                disposable.Dispose();

            Images.Clear();
            Polygons.Clear();
            Segments.Clear();
            PreImageSegments.Clear();
            Vertices.Clear();
            Disposables.Clear();
        }
    }
}
