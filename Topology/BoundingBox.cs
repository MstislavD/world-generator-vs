namespace Topology
{
    public class BoundingBox
    {
        public double MinX { get; set; }
        public double MinY { get; set; }
        public double MaxX { get; set; }
        public double MaxY { get; set; }

        public BoundingBox(double minX, double minY, double maxX, double maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        public bool Contains(Vector2 point)
        {
            return point.X >= MinX && point.X <= MaxX &&
                   point.Y >= MinY && point.Y <= MaxY;
        }

        public bool Intersects(BoundingBox other)
        {
            return !(MinX > other.MaxX || MaxX < other.MinX ||
                     MinY > other.MaxY || MaxY < other.MinY);
        }

        public override string ToString()
        {
            return($"({MinX:F2}; {MinY:F2}) - ({MaxX:F2}; {MaxY:F2})");
        }
    }
}
