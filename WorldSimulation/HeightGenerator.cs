using Topology;
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

        public static void Generate<TGen, TGrid, TCell, TEdge>(TGen generator, TGrid grid, RandomExt random)
            where TGen:IGeneratorCell<TCell>, IGeneratorEdge<TEdge>
            where TGrid:IGrid<TCell>
            where TCell: INode<TCell>, INode<TCell, TEdge>
        {
            Func<TCell, TCell, bool> _ridgeBetween = (c1, c2) => generator.HasRidge(c1.GetEdgeByNeighbor(c2));

            List<TCell> shoreCells = grid.Cells.Where(generator.IsLand).Where(generator.NearSea).ToList();
            List<TEdge> destroyedEdges = new List<TEdge>();

            WeightedTree<TCell> bag = new WeightedTree<TCell>();

            foreach (TCell cell in shoreCells)
            {
                double w1 = cell.Neighbors.Where(generator.IsSea).All(n => _ridgeBetween(cell, n)) ? _noRidgeDelta : 0;
                double weight = Math.Pow(_heightFactor, Elevation.Lowland - generator.GetElevation(cell) - w1);
                bag.Add(cell, weight);
            }

            int height = 0;
            while (bag.Count > 0)
            {
                height += 1;
                TCell cell = bag.Extract(random);
                generator.SetHeight(cell, height);

                IEnumerable<TCell> lowerNeighbors = cell.Neighbors.Where(n => generator.IsSea(n) || generator.GetHeight(n) > 0);
                if (lowerNeighbors.All(n => _ridgeBetween(n, cell)))
                {
                    List<TEdge> edges = lowerNeighbors.Select(cell.GetEdgeByNeighbor).ToList();
                    TEdge ridgeDestroyed = random.NextItemExtract(edges);
                    generator.SetRidge(ridgeDestroyed, false);
                    destroyedEdges.Add(ridgeDestroyed);
                }

                foreach (TCell neighbor in cell.Neighbors.Where(generator.IsLand).Where(n => generator.GetHeight(n) == 0))
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

            foreach (TCell cell in shoreCells)
            {
                double weight = Math.Pow(_heightFactor, (double)generator.GetElevation(cell));
                bag.Add(cell, weight);
            }

            height = 0;
            while (bag.Count > 0)
            {
                height -= 1;
                TCell cell = bag.Extract(random);
                generator.SetHeight(cell, height);

                foreach (TCell neighbor in cell.Neighbors.Where(generator.IsSea).Where(n => generator.GetHeight(n) == 0).Where(n => !bag.Contains(n)))
                {
                    double weight = Math.Pow(_heightFactor, (double)generator.GetElevation(cell));
                    bag.Add(neighbor, weight);
                }
            }
        }

        public static void Generate(WorldGeneratorLegacy generator)
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
