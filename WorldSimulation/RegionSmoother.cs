using Topology;

namespace WorldSimulation
{
    // this class should be turned into generic but currently using WorldGenerator that is not generic itself.
    class RegionSmoother<TGen, TGrid, TCell, TEdge>
        where TGen : IGeneratorCell<TCell>, IGeneratorEdge<TEdge>
        where TGrid : IHexGrid, IGrid<TCell>
        where TCell : ICell<TCell, TEdge>
    {
        public static void Smooth(TGen generator, TGrid grid)
        {
            grid.UpdateVertices((v, c1, c2, c3) => SmoothVertex(generator, grid, v, c1, c2, c3));
        }

        public static Vector2 SmoothVertex(TGen generator, TGrid grid, Vector2 vertex, TCell c1, TCell c2, TCell c3)
        {
            if (c2 == null || c3 == null)
                return vertex.Y < grid.Height / 2 ? new(vertex.X, 0) : new(vertex.X, grid.Height);

            TCell? smoothed = _smoothedHex(c1, c2, c3, (c1, c2) => generator.IsLand(c1) == generator.IsLand(c2));
            if (smoothed == null) smoothed = _smoothedHex(c1, c2, c3, (c1, c2) => generator.GetCellParent(c1).Equals(generator.GetCellParent(c2)));
            if (smoothed == null) smoothed = _smoothedHex(c1, c2, c3, (c1, c2) => !generator.HasRidge(c1.GetEdgeByNeighbor(c2)));

            return smoothed != null ? _moveVertex(grid, vertex, smoothed.Center) : vertex;
        }

        static TCell? _smoothedHex(TCell c1, TCell c2, TCell c3, Func<TCell, TCell, bool> value)
        {
            if (c1 == null || c2 == null || c3 == null)
                return default;

            bool v1 = value(c1, c2);
            bool v2 = value(c1, c3);
            bool v3 = value(c2, c3);

            if (v1.Equals(v2))
                return v1.Equals(v3) ? default : c1;
            else if (v1.Equals(v3))
                return c2;
            else
                return v2.Equals(v3) ? c3 : default;
        }

        static Vector2 _moveVertex(TGrid grid, Vector2 v, Vector2 target)
        {
            // assuming wrapping by y axis
            double dx = target.X - v.X;
            if (dx < -grid.Width / 2)
            {
                dx += grid.Width;
            }
            if (dx > grid.Width / 2)
            {
                dx -= grid.Width;
            }
            double x = dx * 0.25 + v.X;
            double y = (target.Y - v.Y) * 0.25 + v.Y;
            return new(x, y);
        }
    }
}
