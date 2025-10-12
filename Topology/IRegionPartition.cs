using Topology;
using System.Collections.Generic;

namespace PointLocation
{
    public interface IRegionPartition<TRegion>
    {
        IEnumerable<TRegion> Regions { get; }
        IEnumerable<LineSegment> Edges(TRegion region);
        double Top { get; }
        double Bottom { get; }
        double Left { get; }
        double Right { get; }
        double Epsilon { get; }
    }
}
