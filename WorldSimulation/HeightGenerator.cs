using Topology;
using RandomExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;


namespace WorldSimulation
{
    class HeightGenerator
    {
        const double _heightFactor = 10;
        const double _noRidgeDelta = 2;

        public static void Generate(WorldGenerator generator, Grid grid, RandomExt random)
        {
            Func<HexCell, HexCell, bool> _ridgeBetween = (c1, c2) => generator.HasRidge(c1.GetEdgeByNeighbor(c2));

            List<HexCell> shoreCells = grid.Cells.Where(generator.IsLand).Where(generator.NearSea).ToList();
            List<Edge> destroyedEdges = new List<Edge>();

            WeightedBag<HexCell> bag = new WeightedBag<HexCell>();

            foreach (HexCell cell in shoreCells)
            {
                double w1 = cell.Neighbors.Where(generator.IsSea).All(n => _ridgeBetween(cell, n)) ? _noRidgeDelta : 0;
                double weight = Math.Pow(_heightFactor, Elevation.Lowland - generator.GetElevation(cell) - w1);
                bag.Add(cell, weight);
            }

            int height = 0;
            while (bag.Count > 0)
            {
                height += 1;
                HexCell cell = bag.Extract(random);
                generator.CellData[cell].Height = height;

                IEnumerable<HexCell> lowerNeighbors = cell.Neighbors.Where(n => generator.IsSea(n) || generator.GetHeight(n) > 0);
                if (lowerNeighbors.All(n => _ridgeBetween(n, cell)))
                {
                    List<Edge> edges = lowerNeighbors.Select(n => cell.GetEdgeByNeighbor(n)).ToList();
                    Edge ridgeDestroyed = random.NextItemExtract(edges);
                    generator.EdgeData[ridgeDestroyed].Ridge = false;
                    destroyedEdges.Add(ridgeDestroyed);
                }

                foreach (HexCell neighbor in cell.Neighbors.Where(generator.IsLand).Where(n => generator.GetHeight(n) == 0))
                {
                    double w1 = _ridgeBetween(cell, neighbor) ? _noRidgeDelta : 0;
                    double weight = Math.Pow(_heightFactor, Elevation.Lowland - generator.GetElevation(neighbor) - w1);
                    if (!bag.Contains(neighbor) || bag.GetWeight(neighbor) < weight)
                    {
                        bag.Add(neighbor, weight);
                    }
                }
            }

            shoreCells = grid.Cells.Where(generator.IsSea).Where(generator.NearLand).ToList();

            foreach (HexCell cell in shoreCells)
            {
                double weight = Math.Pow(_heightFactor, (double)generator.GetElevation(cell));
                bag.Add(cell, weight);
            }

            height = 0;
            while (bag.Count > 0)
            {
                height -= 1;
                HexCell cell = bag.Extract(random);
                generator.CellData[cell].Height = height;

                foreach (HexCell neighbor in cell.Neighbors.Where(generator.IsSea).Where(n => generator.GetHeight(n) == 0).Where(n => !bag.Contains(n)))
                {
                    double weight = Math.Pow(_heightFactor, (double)generator.GetElevation(cell));
                    bag.Add(neighbor, weight);
                }
            }
        }

        public static void Generate(WorldGenerator generator)
        {
            Dictionary<Elevation, int> countByElevation = new Dictionary<Elevation, int>();
            foreach (Region region in generator.RegionMap.Regions)
            {
                if (!region.IsRidge)
                {
                    Elevation elevation = generator.GetElevation(region.Cell);
                    if (!countByElevation.ContainsKey(elevation))
                        countByElevation[elevation] = 0;
                    countByElevation[elevation] += 1;
                }
            }

            Dictionary<double, double> heightByPosition = new Dictionary<double, double>();
            heightByPosition[0] = 0;
            heightByPosition[countByElevation[Elevation.Lowland]] = 100;
            heightByPosition[countByElevation[Elevation.Upland] + countByElevation[Elevation.Lowland]] = 750;
            heightByPosition[countByElevation.Where(p => p.Key > Elevation.ShallowOcean).Sum(p => p.Value)] = 2500;

            foreach (Region region in generator.RegionMap.Regions.Where(r => r.IsFlat))
            {
                region.Height = Interpolation.Interpolate(heightByPosition, region.Height);
                if (region.Height > 2000)
                    region.Biome = Biomes.Mountains;
            }
        }
    }
}
