using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexGrid;
using RandomExtension;

namespace WorldSimulation
{
    class RiverGenerator
    {
        const double mountainPrecipitationMultiplier = 1.0;
        
        static bool _printLog = false;

        public static void GenerateForRegions(WorldGenerator generator, RandomExt random)
        {
            RegionMap map = generator.RegionMap;

            foreach (Region ridge in map.Regions.Where(r => r.IsRidge))
            {               
                ridge.Drainage = random.NextItemExtract(ridge.FlatNeighbors.ToList());
                double variance = random.NextDouble() * 0.2 + 0.9;
                double precipitation = ridge.FlatNeighbors.Average(c => _precipitation(c) / c.Size);
                ridge.Water += precipitation * mountainPrecipitationMultiplier * variance * ridge.Size;
                ridge.Drainage.Water += ridge.Water;
            }

            foreach (Region land in map.Regions.Where(r => r.IsFlat).OrderByDescending(r => r.Height))
            {
                double variance = random.NextDouble() * 0.2 + 0.9;
                double precipitation = _precipitation(land) * variance * land.Size;
                IEnumerable<Region> lowerNeighbors = land.CellNeighbors.Where(n => n.Height < land.Height);

                List<Region> seaNeighbors = lowerNeighbors.Where(r => r.IsSea).ToList();

                Region drainageRegion = random.NextItem(seaNeighbors.Count > 0 ? seaNeighbors : lowerNeighbors.ToList());
                land.Drainage = drainageRegion;
                land.Water += precipitation;
                drainageRegion.Water += land.Water;
            }

            List<Region> riverEnds = map.Regions.Where(r => !r.IsSea).Where(r => r.Drainage.IsSea).ToList();
            int bigRiverCount = (int)(generator.Parameters.RiverPct.Current.DoubleValue * riverEnds.Count);
            List<Region> bigRiverEnds = riverEnds.OrderByDescending(r => r.Water).Take(bigRiverCount).ToList();
            double minRiverWater = bigRiverEnds.Last().Water * generator.Parameters.TributaryThreshold;

            foreach (Region region in bigRiverEnds)
            {
                _makeRiver(region, minRiverWater);
            }

            if (_printLog)
            {
                Console.WriteLine($"Total rivers: {riverEnds.Count}");
                Console.WriteLine($"Big rivers: {bigRiverCount}");
                Console.WriteLine($"Min river water: {minRiverWater}");
            }
        }

        public static void GenerateForSubregions(WorldGenerator generator, RandomExt random)
        {
            SubregionGraph graph = generator.SubregionGraph;
            RegionMap map = generator.RegionMap;

            List<Subregion> candidates;
            List<Subregion> flooded;
            Dictionary<Subregion, int> heightBySubregion;

            foreach (Region region in map.Regions.Where(r => r.River))
            {
                Region drainageRegion = region.Drainage;
                candidates = region.Subregions.Where(s => s.Neighbors.Any(n => n.Region == drainageRegion)).ToList();
                Subregion lowestSubregion = random.NextItem(candidates);

                candidates = lowestSubregion.Neighbors.Where(n => n.Region == drainageRegion).ToList();
                lowestSubregion.Drainage = random.NextItem(candidates);

                if (region.IsFlat)
                {
                    flooded = Grid.Flood(lowestSubregion, s => s.Region == region).ToList();
                    heightBySubregion = Enumerable.Range(0, flooded.Count).ToDictionary(i => flooded[i]);

                    foreach (Subregion subregion in flooded.OrderByDescending(c => heightBySubregion[c]).Take(flooded.Count - 1))
                    {
                        candidates = subregion.Neighbors.Where(n => n.Region == region && heightBySubregion[n] < heightBySubregion[subregion]).ToList();
                        subregion.Drainage = random.NextItem(candidates);
                    }
                }
            }

            foreach (Region region in map.Regions.Where(r => r.River))
            {
                bool hasRiverStart = true;
                foreach (Subregion subregion in region.Subregions)
                {
                    if (subregion.Neighbors.Any(n => n.Region != region && n.Region.River && n.Drainage == subregion))
                    {
                        hasRiverStart = false;
                        _propagateRiver(subregion);
                    }
                }

                if (hasRiverStart)
                {
                    Subregion riverStart = random.NextItem(region.Subregions.Where(s => s.Drainage != null).ToList());
                    _propagateRiver(riverStart);
                }
            }
        }

        private static void _makeRiver(Region region, double minWater)
        {
            region.River = true;

            if (region.IsRidge)
                return;

            List<Region> rivers = region.AllNeighbors.Where(n => n.Drainage == region).ToList();
            double cellRiverWater = rivers.Count > 0 ? rivers.Max(r => r.Water) : 0;

            foreach (Region river in rivers)
                if (river.Water == cellRiverWater || river.Water > minWater)
                    _makeRiver(river, minWater);
        }

        private static void _propagateRiver(Subregion subregion)
        {
            subregion.River = true;
            if (subregion.Drainage != null && subregion.Region == subregion.Drainage.Region)
                _propagateRiver(subregion.Drainage);
        }

        public static double _precipitation(Region region)
        {
                if (region.Humidity == Humidity.Dry)
                    return 0.1;
                else if (region.Humidity == Humidity.Seasonal)
                    return 0.3;
                else
                    return 0.9;
        }
    }
}
