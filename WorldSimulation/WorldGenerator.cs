using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topology;
using Utilities;

namespace WorldSimulation
{
    public class WorldCell : LayerHexCell<WorldCell, WorldEdge>
    {
        public Elevation Elevation { get; internal set; } = Elevation.DeepOcean;
    }
    public class WorldEdge : LayerEdge<WorldCell, WorldEdge> { }
    public class WorldGrid : HexGrid<WorldCell, WorldEdge>
    {
        public WorldGrid(int columns, int rows) : base(columns, rows) { }
    }

    public class WorldGenerator : IFactoryGrid<WorldGrid>
    {
        int seed = 0;
        double seaPct = 0.7;
        List<WorldGrid> grids = [];
        public int GridLevels { get; } = 7;
        public WorldGrid? Grid(int level) => grids.Count > level ? grids[level] : null;

        public void Generate()
        {
            Random rng = new Random(seed);
            RandomExt rng_e = new RandomExt(seed);

            grids.Clear();
            grids.Add(new WorldGrid(10, 7));
            GenerateRandom(grids[0], rng, seaPct);

            for (int i = 0; i < GridLevels - 1; i++)
            {
                WorldGrid grid = ChildGridGenerator.CreateChildGrid<WorldGrid, WorldCell, WorldEdge>(grids[i], this, rng_e);
                GenerateFromParent(grid);
                grids.Add(grid);
            }
        }

        public void Regenerate()
        {
            seed = new Random().Next();
        }

        public static void GenerateRandom(WorldGrid grid, Random random, double seaPct)
        {
            int landCount = (int)(grid.CellCount * (1 - seaPct));
            WorldCell[] landCells = random.GetItems(grid.Cells.ToArray(), landCount);
            foreach (WorldCell cell in landCells)
            {
                cell.Elevation = Elevation.Lowland;
            }
        }

        public static void GenerateFromParent(WorldGrid childGrid)
        {
            foreach (WorldCell cell in childGrid.Cells)
            {
                WorldCell parent = cell.Parent ?? throw new Exception();
                cell.Elevation = parent.Elevation;
            }
            foreach (WorldEdge edge in childGrid.Edges.Where(e => e.Parent != null))
            {
                
            }
        }

        public WorldGrid CreateGrid(int columns, int rows) => new WorldGrid(columns, rows);
    }
}
