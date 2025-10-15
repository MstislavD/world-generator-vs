using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Topology;
using Utilities;
using WorldSimulation;

namespace WorldSimulation
{
    class ElevationGenerator
    {
        public static void GenerateRandom<TGen, TGrid, TCell, TEdge>(TGen generator, TGrid grid, RandomExt random)
            where TGen : IGenerator, IGeneratorCell<TCell>, IGeneratorEdge<TEdge>
            where TGrid : IGrid<TCell>, IEdges<TEdge>
        {
            List<TCell> cells = grid.Cells.ToList();
            int landCount = grid.CellCount - (int)(generator.SeaPct * grid.CellCount);
            for (int i = 0; i < landCount; i++)
            {
                TCell cell = random.NextItemExtract(cells);
                generator.SetElevation(cell, Elevation.Lowland);
            };

            _createShallowSeas(generator, grid, random);
            _riseLand(generator, grid, random);
            _createRidges(generator, grid, random);
        }

        public static void GenerateFromParent<TGen, TGrid, TCell, TEdge>(TGen generator, IContainer<TGrid, TCell> expandedGrid)
            where TGen : IGeneratorCell<TCell>, IGeneratorEdge<TEdge>
            where TGrid : IGrid<TCell>, IEdges<TEdge>
            where TCell : HexCell<TCell, TEdge>
            where TEdge : Edge<TCell>
        {
            TGrid grid = expandedGrid.Grid;

            foreach (TCell cell in grid.Cells)
            {
                TCell parent = expandedGrid.Getparent(cell);
                generator.SetParent(cell, parent);
                generator.SetElevation(cell, generator.GetElevation(parent));
                generator.SetHeight(cell, generator.GetHeight(parent));
            }
            foreach (TEdge edge in grid.Edges)
            {
                if (edge.Cell2 != null)
                {
                    TCell p1 = generator.GetCellParent(edge.Cell1);
                    TCell p2 = generator.GetCellParent(edge.Cell2);
                    if (!p1.Equals(p2))
                    {
                        TEdge parent = p1.GetEdgeByNeighbor(p2);
                        generator.SetParent(edge, parent);
                        generator.SetRidge(edge, generator.GetRidge(parent));
                    }
                }
            }
        }

        public static void GenerateModify<TGen, TGrid, TCell, TEdge>(TGen generator, TGrid grid, RandomExt random)
            where TGen : IGenerator, IGeneratorCell<TCell>, IGeneratorEdge<TEdge>
            where TGrid : IGrid<TCell>, IEdges<TEdge>
            where TCell : INeighbors<TCell>
        {
            _createIslands(generator, grid, random);
            _riseLand(generator, grid, random);
            _lowerLand(generator, grid, random);
            _destroyRidges(generator, grid, random);
            _createRidges(generator, grid, random);
        }

        private static void _createShallowSeas<TGen, TCell>(TGen generator, IGrid<TCell> grid, RandomExt random)
            where TGen : IGenerator, IGeneratorCell<TCell>
        {
            List<TCell> seaCells = grid.Cells.Where(generator.IsSea).ToList();
            int shallowCount = seaCells.Count - (int)(generator.Parameters.DeepPct.Current * seaCells.Count);
            for (int i = 0; i < shallowCount; i++)
            {
                TCell cell = random.NextItemExtract(seaCells);
                generator.SetElevation(cell, Elevation.ShallowOcean);
            }
        }

        public static void GenerateScriptPangea<TGen, TGrid, TCell, TEdge>(TGen generator, TGrid grid, RandomExt random)
            where TGen : IGenerator, IGeneratorCell<TCell>, IGeneratorEdge<TEdge>
            where TGrid : IGrid<TCell>, IEdges<TEdge>
            where TCell : HexCell<TCell, TEdge>
            where TEdge : Edge<TCell>
        {
            int center = grid.Columns / 2;
            int landCount = grid.CellCount - (int)(generator.SeaPct * grid.CellCount);
            int smallLands = random.Next((int)(landCount * 0.2));
            int pangeaCells = landCount - smallLands;

            TCell newCell = grid.GetCell(center, random.Next(grid.Rows));

            List<TCell> cellPool = [newCell];

            for (int i = 0; i < pangeaCells; i++)
            {
                while (generator.IsLand(newCell))
                    newCell = random.NextItemExtract(cellPool);

                generator.SetElevation(newCell , Elevation.Lowland);
                cellPool.AddRange(newCell.Neighbors.Where(generator.IsSea));
            }

            List<TCell> cells = grid.Cells.Where(generator.IsSea).ToList();
            for (int i = 0; i < smallLands; i++)
            {
                TCell cell = random.NextItemExtract(cells);
                generator.SetElevation(cell, Elevation.Lowland);
            };

            _createShallowSeas(generator, grid, random);
            _riseLand(generator, grid, random);
            _createRidges(generator, grid, random);
        }

        public static void GenerateScriptTwoContinents(WorldGenerator generator, Topology.HexGrid grid, RandomExt random)
        {
            HashSet<HexCell> continentCells = new HashSet<HexCell>();

            int landCount = grid.CellCount - (int)(generator.SeaPct * grid.CellCount);
            double variation = random.NextDouble() * 0.2;
            int c1 = (int)((variation + 0.5) * landCount);
            int c2 = landCount - c1;

            Console.WriteLine($"Continent 1: {c1}, Continent 2: {c2}");

            HexCell newCell = random.NextItemExtract(grid.Cells.ToList());
            List<HexCell> cellPool = new List<HexCell>() { newCell };

            for (int i = 0; i < c1; i++)
            {
                while (generator.IsLand(newCell))
                    newCell = random.NextItemExtract(cellPool);

                generator.CellData[newCell].Elevation = Elevation.Lowland;
                continentCells.Add(newCell);
                cellPool.AddRange(newCell.Neighbors.Where(generator.IsSea));
            }

            Func<HexCell, bool> validCell = c => generator.IsSea(c) && c.Neighbors.All(n => !continentCells.Contains(n));

            newCell = random.NextItemExtract(grid.Cells.Where(validCell).ToList());
            cellPool = new List<HexCell>() { newCell };

            for (int i = 0; i < c2; i++)
            {
                while (generator.IsLand(newCell) && cellPool.Count > 0)
                    newCell = random.NextItemExtract(cellPool);

                generator.CellData[newCell].Elevation = Elevation.Lowland;
                cellPool.AddRange(newCell.Neighbors.Where(validCell));
            }

            _createShallowSeas(generator, grid, random);
            _riseLand(generator, grid, random);
            _createRidges(generator, grid, random);
        }

        public static void GenerateScriptThreeContinents(WorldGenerator generator, Topology.HexGrid grid, RandomExt random)
        {
            HashSet<HexCell> continentCells = new HashSet<HexCell>();
            HashSet<HexCell> continentCellsTmp = new HashSet<HexCell>();

            int landCount = grid.CellCount - (int)(generator.SeaPct * grid.CellCount);
            double variation = random.NextDouble() * 0.1;
            int c1 = (int)((variation + 0.33) * landCount);
            variation = random.NextDouble() * 0.2;
            int c2 = (int)((variation + 0.5) * (landCount - c1));
            int c3 = landCount - c1 - c2;

            Console.WriteLine($"Continent 1: {c1}, Continent 2: {c2}, Continent 3: {c3}");

            HexCell newCell = random.NextItemExtract(grid.Cells.ToList());
            List<HexCell> cellPool = new List<HexCell>() { newCell };

            for (int i = 0; i < c1; i++)
            {
                while (generator.IsLand(newCell) && cellPool.Count > 0)
                    newCell = random.NextItemExtract(cellPool);

                generator.CellData[newCell].Elevation = Elevation.Lowland;
                continentCells.Add(newCell);
                cellPool.AddRange(newCell.Neighbors.Where(generator.IsSea));
            }

            Func<HexCell, bool> validCell = c => generator.IsSea(c) && c.Neighbors.All(n => !continentCells.Contains(n));

            newCell = random.NextItemExtract(grid.Cells.Where(validCell).ToList());
            cellPool = new List<HexCell>() { newCell };

            for (int i = 0; i < c2; i++)
            {
                while (generator.IsLand(newCell) && cellPool.Count > 0)
                    newCell = random.NextItemExtract(cellPool);


                generator.CellData[newCell].Elevation = Elevation.Lowland;
                continentCellsTmp.Add(newCell);
                cellPool.AddRange(newCell.Neighbors.Where(validCell));
            }

            continentCells.UnionWith(continentCellsTmp);

            newCell = random.NextItemExtract(grid.Cells.Where(validCell).ToList());
            cellPool = new List<HexCell>() { newCell };

            for (int i = 0; i < c3; i++)
            {
                while (generator.IsLand(newCell) && cellPool.Count > 0)
                    newCell = random.NextItemExtract(cellPool);

                generator.CellData[newCell].Elevation = Elevation.Lowland;
                cellPool.AddRange(newCell.Neighbors.Where(validCell));
            }

            _createShallowSeas(generator, grid, random);
            _riseLand(generator, grid, random);
            _createRidges(generator, grid, random);
        }

        static void _riseLand<TGen, TCell>(TGen generator, IGrid<TCell> grid, RandomExt random)
            where TGen : IGenerator, IGeneratorCell<TCell>
        {
            List<TCell> landCells = grid.Cells.Where(generator.IsLand).ToList();
            int count = (int)(landCells.Count * generator.Parameters.RisePct);

            while (count > 0 && landCells.Count > 0)
            {
                TCell cell = random.NextItemExtract(landCells);
                generator.ChangeElevationLevel(cell, 1);
                count -= 1;
            }
        }

        static void _lowerLand<TGen, TCell>(TGen generator, IGrid<TCell> grid, RandomExt random)
            where TGen : IGenerator, IGeneratorCell<TCell>
        {
            List<TCell> landCells = grid.Cells.Where(c => generator.GetElevation(c) > Elevation.Lowland).ToList();
            int count = (int)(landCells.Count * generator.Parameters.LowerPct);

            while (count > 0 && landCells.Count > 0)
            {
                TCell cell = random.NextItemExtract(landCells);
                generator.ChangeElevationLevel(cell, -1);
                count -= 1;
            }
        }

        static void _createRidges<TGen, TEdge>(TGen generator, IEdges<TEdge> grid, RandomExt random)
            where TGen : IGenerator, IGeneratorEdge<TEdge>
        {
            List<TEdge> edges = grid.Edges.Where(generator.PossibleRidge).ToList();
            int count = (int)(edges.Count * generator.Parameters.RidgePct.Current);
            for (int i = 0; i < count; i++)
            {
                TEdge edge = random.NextItemExtract(edges);
                generator.SetRidge(edge, true);
            };
        }

        static void _destroyRidges<TGen, TEdge>(TGen generator, IEdges<TEdge> grid, RandomExt random)
            where TGen : IGenerator, IGeneratorEdge<TEdge>
        {
            List<TEdge> edges = grid.Edges.Where(generator.HasRidge).ToList();
            int ridgeCount = (int)(edges.Count * generator.Parameters.RidgeClearPct.Current);
            for (int i = 0; i < ridgeCount; i++)
            {
                TEdge edge = random.NextItemExtract(edges);
                generator.SetRidge(edge, false);
            };
        }

        static void _createIslands<TGen, TCell>(TGen generator, IGrid<TCell> grid, RandomExt random)
            where TGen : IGenerator, IGeneratorCell<TCell>
            where TCell : INeighbors<TCell>
        {
            int islandCount = (int)(grid.Cells.Count(generator.IsSea) * generator.Parameters.IslandPct.Current);

            Func<TCell, bool> noLandAround = c => !c.Neighbors.Any(generator.IsLand);
            Func<TCell, bool> isShallow = c => generator.GetElevation(c) == Elevation.ShallowOcean;
            Func<TCell, bool> isNotLandConnection = c => !Geometry.IsConnection(c, generator.IsLand);

            List<TCell> shallowSeas = grid.Cells.Where(isShallow).Where(noLandAround).ToList();

            while (islandCount > 0 && shallowSeas.Count > 0)
            {
                TCell cell = random.NextItemExtract(shallowSeas);
                if (noLandAround(cell))
                {
                    generator.SetElevation(cell, Elevation.Lowland);
                    islandCount -= 1;
                }
            }

            int targetLandCount = grid.CellCount - (int)(generator.SeaPct * grid.CellCount);
            int currentLandCount = grid.Cells.Where(generator.IsLand).Count();

            List<TCell> cells = grid.Cells.Where(generator.IsLand).Where(generator.NearSea).Where(isNotLandConnection).ToList();

            while (targetLandCount < currentLandCount)
            {
                TCell cell = random.NextItemExtract(cells);
                if (isNotLandConnection(cell))
                {
                    currentLandCount -= 1;
                    generator.SetElevation(cell, Elevation.ShallowOcean);
                }
            }
        }

        static void _createMountainRegions(WorldGenerator generator, HexGrid grid, RandomExt random)
        {
            foreach (Edge edge in grid.Edges.Where(generator.HasRidge))
            {
                List<HexCell> pool = edge.Cells.Where(generator.IsLand).ToList();
                if (pool.Count > 0 && !pool.Any(c => generator.GetElevation(c) == Elevation.Mountain))
                {
                    HexCell mnt = random.NextItemExtract(pool);
                    generator.CellData[mnt].Elevation = Elevation.Mountain;
                }
            }
        }
    }
}
