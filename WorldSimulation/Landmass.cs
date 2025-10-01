using HexGrid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldSimulation
{
    public class Landmass
    {
        WorldGenerator _generator;
        List<Region> _regions;
        List<Landmass> _neighbors;

        public Landmass(WorldGenerator generator,int seed)
        {
            _generator = generator;
            _regions = new List<Region>();
            _neighbors = new List<Landmass>();
            Seed = seed;
        }

        public int RegionCount => _regions.Count;
        public int Size => _regions.Sum(r => r.Size);
        public IEnumerable<Region> Regions => _regions;
        public IEnumerable<Landmass> Neighbors => _neighbors;
        public int Seed { get; private set; }
        public Vertex Center
        {
            get
            {
                Vertex center = new Vertex(0, 0);
                Vertex worldShift = _generator.SubregionGraph.Width * new Vertex(1, 0);
                double halfWidth = _generator.SubregionGraph.Width / 2;
                int totalSize = 0;

                foreach(Region region in _regions)
                {
                    Vertex regionCenter = new Vertex(region.Center);                   

                    if (totalSize > 0)
                    {
                        double deltaX = regionCenter.X - center.X;
                        if (deltaX > halfWidth)
                            regionCenter -= worldShift;
                        else if (deltaX < -halfWidth)
                            regionCenter += worldShift;
                    }

                    center = totalSize * center + region.Size * regionCenter;
                    totalSize = totalSize + region.Size;
                    center = 1.0 / totalSize * center;

                    if (center.X > _generator.SubregionGraph.Width)
                        center -= worldShift;
                    else if (center.X < 0)
                        center += worldShift;
                }               


                return center;
            }
        }

        internal void AddRegion(Region region) => _regions.Add(region);
        internal void AddNeighbor(Landmass neighbor) => _neighbors.Add(neighbor);

    }
}
