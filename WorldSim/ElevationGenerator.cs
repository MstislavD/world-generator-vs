using HexGrid;
using RandomExtended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldSim
{
    class ElevationGenerator
    {
        public static void GenerateRandom(WorldGenerator generator, Grid grid, RandomExt random)
        {
            List<HexCell> cells = grid.Cells.ToList();
            int landCount = grid.CellCount - (int)(generator.SeaPct * grid.CellCount);
            for (int i = 0; i < landCount; i++)
            {
                HexCell cell = random.NextItemExtract(cells);
                generator.CellData[cell].Elevation = Elevation.Lowland;
            };

            _createShallowSeas(generator, grid, random);
            _riseLand(generator, grid, random);
            _createRidges(generator, grid, random);
        }

        public static void GenerateFromParent(WorldGenerator generator, ExpandedHexGrid expandedGrid)
        {
            Grid grid = expandedGrid.Grid;
            Dictionary<HexCell, CellData> cData = generator.CellData;
            Dictionary<Edge, EdgeData> eData = generator.EdgeData;

            foreach (HexCell cell in grid.Cells)
            {
                HexCell parent = expandedGrid.ParentByCell[cell];
                cData[cell].Parent = parent;
                cData[cell].Elevation = generator.GetElevation(parent);
                cData[cell].Height = generator.GetHeight(parent);
            }
            foreach (Edge edge in grid.Edges)
            {
                if (edge.Cell2 != null)
                {
                    HexCell p1 = generator.GetCellParent(edge.Cell1);
                    HexCell p2 = generator.GetCellParent(edge.Cell2);
                    if (!p1.Equals(p2))
                    {
                        Edge parent = p1.GetEdgeByNeighbor(p2);
                        eData[edge].Parent = parent;
                        eData[edge].Ridge = generator.HasRidge(parent);
                    }
                }
            }
        }

        public static void GenerateModify(WorldGenerator generator, Grid grid, RandomExt random)
        {
            _createIslands(generator, grid, random);
            _riseLand(generator, grid, random);
            _lowerLand(generator, grid, random);
            _destroyRidges(generator, grid, random);
            _createRidges(generator, grid, random);
        }

        private static void _createShallowSeas(WorldGenerator generator, HexGrid.Grid grid, RandomExt random)
        {
            List<HexCell> seaCells = grid.Cells.Where(generator.IsSea).ToList();
            int shallowCount = seaCells.Count - (int)(generator.Parameters.DeepPct.Current.DoubleValue * seaCells.Count);
            for (int i = 0; i < shallowCount; i++)
            {
                HexCell cell = random.NextItemExtract(seaCells);
                generator.CellData[cell].Elevation = Elevation.ShallowOcean;
            };
        }

        public static void GenerateScriptPangea(WorldGenerator generator, HexGrid.Grid grid, RandomExt random)
        {
            int center = grid.Width / 2;
            int landCount = grid.CellCount - (int)(generator.SeaPct * grid.CellCount);
            int smallLands = random.Next((int)(landCount * 0.2));
            int pangeaCells = landCount - smallLands;

            HexCell newCell = random.NextItemExtract(grid.Cells.Where(c => c.GridPositionX == center).ToList());
            List<HexCell> cellPool = new List<HexCell>() { newCell };

            for (int i = 0; i < pangeaCells; i++)
            {
                while (generator.IsLand(newCell))
                    newCell = random.NextItemExtract(cellPool);

                generator.CellData[newCell].Elevation = Elevation.Lowland;
                cellPool.AddRange(newCell.Neighbors.Where(generator.IsSea));
            }

            List<HexCell> cells = grid.Cells.Where(generator.IsSea).ToList();
            for (int i = 0; i < smallLands; i++)
            {
                HexCell cell = random.NextItemExtract(cells);
                generator.CellData[cell].Elevation = Elevation.Lowland;
            };

            _createShallowSeas(generator, grid, random);
            _riseLand(generator, grid, random);
            _createRidges(generator, grid, random);
        }

        public static void GenerateScriptTwoContinents(WorldGenerator generator, HexGrid.Grid grid, RandomExt random)
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

        public static void GenerateScriptThreeContinents(WorldGenerator generator, HexGrid.Grid grid, RandomExt random)
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

        static void _riseLand(WorldGenerator generator, Grid grid, RandomExt random)
        {
            List<HexCell> landCells = grid.Cells.Where(generator.IsLand).ToList();
            int count = (int)(landCells.Count * generator.Parameters.RisePct.Current.DoubleValue);

            while (count > 0 && landCells.Count > 0)
            {
                HexCell cell = random.NextItemExtract(landCells);
                generator.CellData[cell].Elevation += 1;
                count -= 1;
            }
        }

        static void _lowerLand(WorldGenerator generator, Grid grid, RandomExt random)
        {
            List<HexCell> landCells = grid.Cells.Where(c => generator.GetElevation(c) > Elevation.Lowland).ToList();
            int count = (int)(landCells.Count * generator.Parameters.LowerPct.Current.DoubleValue);

            while (count > 0 && landCells.Count > 0)
            {
                HexCell cell = random.NextItemExtract(landCells);
                generator.CellData[cell].Elevation -= 1;
                count -= 1;
            }
        }

        static void _createRidges(WorldGenerator generator, Grid grid, RandomExt random)
        {
            List<Edge> edges = grid.Edges.Where(generator.PossibleRidge).ToList();
            int count = (int)(edges.Count * generator.Parameters.RidgePct.Current.DoubleValue);
            for (int i = 0; i < count; i++)
            {
                Edge edge = random.NextItemExtract(edges);
                generator.EdgeData[edge].Ridge = true;
            };
        }

        static void _destroyRidges(WorldGenerator generator, Grid grid, RandomExt random)
        {
            List<Edge> edges = grid.Edges.Where(generator.HasRidge).ToList();
            int ridgeCount = (int)(edges.Count * generator.Parameters.RidgeClearPct.Current.DoubleValue);
            for (int i = 0; i < ridgeCount; i++)
            {
                Edge edge = random.NextItemExtract(edges);
                generator.EdgeData[edge].Ridge = false;
            };
        }

        static void _createIslands(WorldGenerator generator, Grid grid, RandomExt random)
        {
            int islandCount = (int)(grid.Cells.Count(generator.IsSea) * generator.Parameters.IslandPct.Current.DoubleValue);

            Func<HexCell, bool> noLandAround = c => !c.Neighbors.Any(generator.IsLand);
            Func<HexCell, bool> isShallow = c => generator.GetElevation(c) == Elevation.ShallowOcean;
            Func<HexCell, bool> isNotLandConnection = c => !Grid.IsConnection(c, generator.IsLand);

            List<HexCell> shallowSeas = grid.Cells.Where(isShallow).Where(noLandAround).ToList();

            while (islandCount > 0 && shallowSeas.Count > 0)
            {
                HexCell cell = random.NextItemExtract(shallowSeas);
                if (noLandAround(cell))
                {
                    generator.CellData[cell].Elevation = Elevation.Lowland;
                    islandCount -= 1;
                }
            }

            int targetLandCount = grid.CellCount - (int)(generator.SeaPct * grid.CellCount);
            int currentLandCount = grid.Cells.Where(generator.IsLand).Count();

            List<HexCell> cells = grid.Cells.Where(generator.IsLand).Where(generator.NearSea).Where(isNotLandConnection).ToList();

            while (targetLandCount < currentLandCount)
            {
                HexCell cell = random.NextItemExtract(cells);
                if (isNotLandConnection(cell))
                {
                    currentLandCount -= 1;
                    generator.CellData[cell].Elevation = Elevation.ShallowOcean;
                }
            }
        }

        //void _createIslands(HexGrid.Grid grid)
        //{
        //    int islandCount = (int)(grid.Cells.Count(IsSea) * _islandPct);

        //    Func<HexCell, bool> noLandAround = c => !c.Neighbors.Any(IsLand);
        //    Func<HexCell, bool> isShallow = c => GetElevation(c) == Elevation.ShallowOcean;
        //    Func<HexCell, bool> isNotLandConnection = c => !IsConnection(c, IsLand);

        //    List<HexCell> shallowSeas = grid.Cells.Where(isShallow).Where(noLandAround).ToList();

        //    while (islandCount > 0 && shallowSeas.Count > 0)
        //    {
        //        HexCell cell = random.NextItemExtract(shallowSeas);
        //        if (noLandAround(cell))
        //        {
        //            _cData[cell].Elevation = Elevation.Lowland;
        //            islandCount -= 1;
        //        }
        //    }

        //    int targetLandCount = grid.CellCount - (int)(SeaPct * grid.CellCount);
        //    int currentLandCount = grid.Cells.Where(IsLand).Count();

        //    List<HexCell> cells = grid.Cells.Where(IsLand).Where(NearSea).Where(isNotLandConnection).ToList();

        //    while (targetLandCount < currentLandCount)
        //    {
        //        HexCell cell = random.NextItemExtract(cells);
        //        if (isNotLandConnection(cell))
        //        {
        //            currentLandCount -= 1;
        //            _cData[cell].Elevation = Elevation.ShallowOcean;
        //        }
        //    }
        //}

        static void _createMountainRegions(WorldGenerator generator, Grid grid, RandomExt random)
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
