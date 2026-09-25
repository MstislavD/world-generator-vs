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
        public WorldGenerationParameters Parameters { get; } = new();

        List<WorldGrid> _grids = [];
        public int GridLevels { get; } = 5;
        public WorldGrid? Grid(int level) => level >= 0 && _grids.Count > level ? _grids[level] : null;
        public bool GenerationIsComplete { get; private set; } = false;

        public event EventHandler OnGenerationComplete = delegate { };

        public void Generate()
        {
            GenerationIsComplete = false;
            RandomExt rng_e = new RandomExt(Parameters.Seed);

            _grids.Clear();
            _grids.Add(new WorldGrid(10, 7));
            GenerateRandom(_grids[0], rng_e, Parameters.SeaPct);

            for (int i = 0; i < GridLevels - 1; i++)
            {
                WorldGrid grid = ChildGridGenerator.CreateChildGrid<WorldGrid, WorldCell, WorldEdge>(_grids[i], this, rng_e);
                GenerateFromParent(grid);
                if (Parameters.SeaToLand) 
                    _seaToLand(grid, rng_e);
                _grids.Add(grid);
            }

            GenerationIsComplete = true;
            OnGenerationComplete.Invoke(this, EventArgs.Empty);
        }

        public void Regenerate()
        {
            Parameters.RegenerateSeeds();
            Generate();
        }

        public void Regenerate(int newSeed)
        {
            Parameters.RegenerateSeeds(newSeed);
            Generate();
        }

        public Elevation GetElevation(WorldCell cell) => cell.Elevation;

        public void SetElevation(WorldCell cell, Elevation elevation) => cell.Elevation = elevation;

        public bool IsLand(WorldCell cell) => cell.Elevation >= Elevation.Lowland;

        public bool IsSea(WorldCell cell) => cell.Elevation < Elevation.Lowland;

        public static void GenerateRandom(WorldGrid grid, RandomExt random, double seaPct)
        {
            if (seaPct < 0 || seaPct > 1) throw new Exception("seaPct parameter should lie in the [0;1] range.");
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

        void _seaToLand(IGrid<WorldCell> grid, RandomExt rng)
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
