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
                    _swapElevations(grid, rng_e);
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

        /// <summary>
        /// Alternately swaps cells between sea and land, starting with sea to land.
        /// The swap budget is a fraction of the level's tiles, ruled by
        /// <see cref="WorldGenerationParameters.SwapPct"/>. A swap is only made from a
        /// pool that still has candidates; if the pool of the current turn is empty
        /// the other direction takes the turn instead, so strict alternation holds as
        /// long as both pools have candidates. Cut vertices are re-checked on every
        /// extraction in both directions, so a swap can never split the sea region nor
        /// the land region it takes from. Cells without same-type neighbors (1-tile
        /// islands and lakes) are never swapped, so no region chunk can be eroded away
        /// entirely: any chunk may shrink, but its last tile is always isolated and
        /// thus unswappable.
        /// </summary>
        void _swapElevations(IGrid<WorldCell> grid, RandomExt rng)
        {
            int budget = (int)(Parameters.SwapPct * grid.CellCount);

            // Two candidate pools: cells that may become land (weighted by adjacent
            // land) and cells that may become sea (weighted by adjacent sea).
            WeightedTree<WorldCell> toLand = new();
            WeightedTree<WorldCell> toSea = new();
            foreach (WorldCell cell in grid.Cells)
            {
                if (IsSea(cell))
                {
                    if (CanSwapToLand(cell))
                    {
                        toLand.Add(cell, Math.Pow(2, cell.Neighbors.Count(IsLand)));
                    }
                }
                else
                {
                    if (CanSwapToSea(cell))
                    {
                        toSea.Add(cell, Math.Pow(2, cell.Neighbors.Count(IsSea)));
                    }
                }
            }

            // Cells swapped during this pass. They are never re-added to the
            // opposite pool, so a cell cannot flip back and forth within one pass.
            HashSet<WorldCell> converted = new();

            int done = 0;
            bool seaToLandTurn = true;
            while (done < budget)
            {
                WeightedTree<WorldCell> tree = seaToLandTurn ? toLand : toSea;
                if (tree.Count == 0)
                {
                    // No candidates in this direction: let the other direction take
                    // the turn, and stop only when both pools are dry.
                    if (toLand.Count == 0 && toSea.Count == 0)
                    {
                        break;
                    }
                    seaToLandTurn = !seaToLandTurn;
                    tree = seaToLandTurn ? toLand : toSea;
                }

                WorldCell cell = tree.Extract(rng);
                cell.Elevation = seaToLandTurn ? Elevation.Lowland : Elevation.DeepOcean;
                converted.Add(cell);
                done += 1;
                seaToLandTurn = !seaToLandTurn;

                // Re-evaluate the flipped cell's neighbors in the pool matching their
                // current type: only they can change eligibility (non-adjacent cells
                // keep both their same-type neighborhood and their opposite-type
                // neighbor count). A full recompute plus upsert/remove covers raised
                // or lowered weights, new candidates, cells that became cut vertices
                // of the shrunken region, and cut vertices the flipped cell just
                // bridged (adding a node can un-make an articulation point).
                foreach (WorldCell neighbor in cell.Neighbors)
                {
                    if (converted.Contains(neighbor))
                    {
                        continue;
                    }

                    if (IsSea(neighbor))
                    {
                        if (CanSwapToLand(neighbor))
                        {
                            toLand.Add(neighbor, Math.Pow(2, neighbor.Neighbors.Count(IsLand)));
                        }
                        else
                        {
                            toLand.Remove(neighbor);
                        }
                    }
                    else
                    {
                        if (CanSwapToSea(neighbor))
                        {
                            toSea.Add(neighbor, Math.Pow(2, neighbor.Neighbors.Count(IsSea)));
                        }
                        else
                        {
                            toSea.Remove(neighbor);
                        }
                    }
                }
            }
        }

        // A cell may be swapped only if it touches both types of terrain and is not
        // a cut vertex of its own region. Touching no same-type neighbor means the
        // cell is a 1-tile island or lake: such tiles survive the pass, which also
        // guarantees that no region chunk can disappear (the last tile of any chunk
        // is isolated and therefore unswappable).
        bool CanSwapToLand(WorldCell cell)
            => cell.Neighbors.Any(IsLand) && cell.Neighbors.Any(IsSea) && !Node.IsConnection(cell, IsSea);

        bool CanSwapToSea(WorldCell cell)
            => cell.Neighbors.Any(IsSea) && cell.Neighbors.Any(IsLand) && !Node.IsConnection(cell, IsLand);

        public WorldGrid CreateGrid(int columns, int rows) => new WorldGrid(columns, rows);
    }
}
