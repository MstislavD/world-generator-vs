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
        Random rng = new Random();
        public int GridLevels { get; } = 5;
        public WorldGrid? Grid(int level) => grids.Count > level ? grids[level] : null;
        public bool GenerationIsComplete { get; private set; } = false;

        public event EventHandler OnGenerationComplete = delegate { };

        public Parameter<bool> SeaToLand { get; } = new Parameter<bool>("Sea to land", true);

        public void Generate()
        {
            rng = new Random(seed);
            RandomExt rng_e = new RandomExt(seed);

            grids.Clear();
            grids.Add(new WorldGrid(10, 7));
            GenerateRandom(grids[0], rng, seaPct);

            for (int i = 0; i < GridLevels - 1; i++)
            {
                WorldGrid grid = ChildGridGenerator.CreateChildGrid<WorldGrid, WorldCell, WorldEdge>(grids[i], this, rng_e);
                GenerateFromParent(grid);
                if (SeaToLand)
                    _seaToLand(grid);
                grids.Add(grid);
            }

            GenerationIsComplete = true;
            OnGenerationComplete.Invoke(this, EventArgs.Empty);
        }

        public void Regenerate()
        {
            seed = new Random().Next();
            Generate();
        }

        public void Regenerate(int newSeed)
        {
            seed = newSeed;
            Generate();
        }

        public Elevation GetElevation(WorldCell cell) => cell.Elevation;

        public void SetElevation(WorldCell cell, Elevation elevation) => cell.Elevation = elevation;

        public bool IsLand(WorldCell cell) => cell.Elevation >= Elevation.Lowland;

        public bool IsSea(WorldCell cell) => cell.Elevation < Elevation.Lowland;

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

        void _seaToLand(IGrid<WorldCell> grid)
        {
            double pct = 0.025;

            WeightedTree<WorldCell> tree = new();
            foreach(WorldCell cell in grid.Cells)
            {
                if (IsSea(cell) && cell.Neighbors.Any(IsLand) && !Node.IsConnection(cell, IsSea))
                {
                    tree.Add(cell, Math.Pow(2, cell.Neighbors.Count(IsLand)));
                }
            }

            int count = (int)(pct * grid.CellCount);

            while (count > 0 && tree.Count > 0)
            {
                WorldCell cell = tree.Extract(rng);
                cell.Elevation = Elevation.Lowland;
                count -= 1;
            }
        }

        public WorldGrid CreateGrid(int columns, int rows) => new WorldGrid(columns, rows);
    }
}
