using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topology;
using Utilities;

namespace WorldSimulation
{
    /// <summary>
    /// A continent: the set of tiles descended from one tile of the base (layer 0) grid.
    /// One is created for every base-layer tile — land and sea alike — and inherited by all
    /// its descendants, so at any layer a cell's Continent identifies the base tile it comes from.
    /// </summary>
    public class Continent
    {
        public int Id { get; }
        public Continent(int id) => Id = id;
    }

    public class WorldCell : LayerHexCell<WorldCell, WorldEdge>
    {
        public Elevation Elevation { get; internal set; } = Elevation.DeepOcean;
        public Continent? Continent { get; internal set; }
    }
    public class WorldEdge : LayerEdge<WorldCell, WorldEdge>
    {
        /// <summary>
        /// True if this edge is a ridge. Assigned randomly to some base-layer edges and
        /// inherited from the parent edge on every subsequent layer.
        /// </summary>
        public bool Ridge { get; set; }
    }
    public class WorldGrid : HexGrid<WorldCell, WorldEdge>
    {
        public WorldGrid(int columns, int rows) : base(columns, rows) { }
    }

    public class WorldGenerator : IFactoryGrid<WorldGrid>
    {
        public WorldGenerationParameters Parameters { get; } = new();

        /// <summary>Target fraction of base-layer edges that become ridges; the exact count is fixed per generation (see Generate).</summary>
        const double RidgePct = 0.1;

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

            // Every base-layer tile — land or sea — gets its own continent. The shuffled order
            // decorrelates the id (and thus the map color) from the tile's position, so adjacent
            // tiles almost always end up with different colors.
            int continentId = 0;
            foreach (WorldCell cell in rng_e.Permutation(_grids[0].Cells.ToList()))
                cell.Continent = new Continent(continentId++);

            GenerateRandom(_grids[0], rng_e, Parameters.SeaPct);

            // A fixed number of base-layer edges become ridges — exactly (int)(edge count * RidgePct),
            // so the count is constant between generations and only the choice of edges varies.
            // Only edges that touch land are eligible. Every subsequent layer inherits the flag
            // from its parent edge (see GenerateFromParent).
            List<WorldEdge> edges = _grids[0].Edges.ToList();
            List<WorldEdge> eligible = edges.Where(e => IsLand(e.Cell1) || (e.Cell2 != null && IsLand(e.Cell2))).ToList();
            int ridgeCount = Math.Min((int)(edges.Count * RidgePct), eligible.Count);
            foreach (WorldEdge edge in rng_e.Permutation(eligible).Take(ridgeCount))
                edge.Ridge = true;

            for (int i = 0; i < GridLevels - 1; i++)
            {
                WorldGrid grid = ChildGridGenerator.CreateChildGrid<WorldGrid, WorldCell, WorldEdge>(_grids[i], this, rng_e);
                GenerateFromParent(grid);

                // The last layer only undergoes the inherit pass — no land/sea swaps.
                if (Parameters.SeaToLand && i + 1 < GridLevels - 1)
                    _swapElevations(grid, rng_e);

                // Mountains are placed on the second-to-last layer — after its swaps, so they survive them —
                // and the last layer inherits the mountain elevation from these cells.
                if (i + 1 == GridLevels - 2)
                    _makeMountains(grid);

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
            // Pick distinct cells without replacement: each extracted cell leaves the
            // pool, so exactly landCount tiles end up as land.
            List<WorldCell> pool = grid.Cells.ToList();
            for (int i = 0; i < landCount; i++)
            {
                WorldCell cell = random.NextItemExtract(pool);
                cell.Elevation = Elevation.Lowland;
            }
        }

        public static void GenerateFromParent(WorldGrid childGrid)
        {
            foreach (WorldCell cell in childGrid.Cells)
            {
                WorldCell parent = cell.Parent ?? throw new Exception();
                cell.Elevation = parent.Elevation;
                cell.Continent = parent.Continent;
            }
            // Boundary edges inherit the ridge flag from their parent; edges that lie entirely
            // inside one parent tile have no parent edge and stay non-ridge.
            foreach (WorldEdge edge in childGrid.Edges.Where(e => e.Parent != null))
                edge.Ridge = edge.Parent.Ridge;
        }

        /// <summary>
        /// On the second-to-last layer, a land cell becomes a mountain when its parent was chosen from exactly two
        /// candidates (see ChildGridGenerator.CreateChildGrid) and those two candidates are joined by a ridge in the
        /// previous layer. Every ridge thus turns into a one-tile-wide chain of mountains on that layer; the last
        /// layer inherits the mountain elevation from these cells.
        /// </summary>
        void _makeMountains(WorldGrid grid)
        {
            foreach (WorldCell cell in grid.Cells)
            {
                if (!IsLand(cell)) continue;

                // The cell's parent must have been chosen from exactly two candidates joined by a ridge.
                List<WorldCell>? candidates = cell.ParentCandidates;
                if (candidates == null || candidates.Count != 2) continue;

                WorldCell a = candidates[0];
                WorldCell b = candidates[1];
                if (!a.Neighbors.Contains(b)) continue;

                if (a.GetEdgeByNeighbor(b).Ridge)
                    cell.Elevation = Elevation.Mountain;
            }
        }

        /// <summary>
        /// Brings the level's land/sea ratio to <see cref="WorldGenerationParameters.SeaPct"/> —
        /// matching the target count is the priority, so this correction is not limited by
        /// the swap budget — then spends what remains of the budget on random two-way swaps
        /// that preserve it. The swap budget is a fraction of the level's tiles, ruled by
        /// <see cref="WorldGenerationParameters.SwapPct"/>; it caps the total number of swaps
        /// across both phases, so the second phase only gets what the correction did not
        /// already consume. First, every swap goes in the single direction that moves the
        /// land count toward its target; this stops when the target is reached or no
        /// candidates remain in that direction (the rest are cut vertices or isolated tiles
        /// and cannot be swapped). Then the rest of the budget is spent in pairs — one sea-to-land and one
        /// land-to-sea swap per pair — so the net effect on the counts is zero and the
        /// ratio established by the first phase is preserved; a lone leftover swap is
        /// left unspent rather than unbalance the counts. A swap is only made from a pool
        /// that still has candidates, and a pair can only be started while both pools do;
        /// if one pool dries up the pass stops instead of taking over in one direction.
        /// Cut vertices are re-checked on every extraction in both directions, so a swap
        /// can never split the sea region nor the land region it takes from. Cells without
        /// same-type neighbors (1-tile islands and lakes) are never swapped, so no region
        /// chunk can be eroded away entirely: any chunk may shrink, but its last tile is
        /// always isolated and thus unswappable.
        /// </summary>
        void _swapElevations(IGrid<WorldCell> grid, RandomExt rng)
        {
            int budget = (int)(Parameters.SwapPct * grid.CellCount);
            int targetLand = (int)(grid.CellCount * (1 - Parameters.SeaPct));

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
            // opposite pool, so a cell cannot flip back and forth within one pass —
            // in particular, phase 2 can never undo a correction made by phase 1.
            HashSet<WorldCell> converted = new();

            // Re-evaluate the flipped cell's neighbors in the pool matching their
            // current type: only they can change eligibility (non-adjacent cells
            // keep both their same-type neighborhood and their opposite-type
            // neighbor count). A full recompute plus upsert/remove covers raised
            // or lowered weights, new candidates, cells that became cut vertices
            // of the shrunken region, and cut vertices the flipped cell just
            // bridged (adding a node can un-make an articulation point).
            void RefreshPools(WorldCell flipped)
            {
                foreach (WorldCell neighbor in flipped.Neighbors)
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

            // Phase 1: reach the target ratio. Matching the target is the priority, so
            // this phase runs until the target is hit or no candidates remain in that
            // direction (the rest are cut vertices or isolated tiles and cannot be
            // swapped); it still draws on the budget, leaving less for phase 2.
            int needed = targetLand - grid.Cells.Count(IsLand);
            bool growLand = needed > 0;
            while (needed != 0)
            {
                WeightedTree<WorldCell> tree = growLand ? toLand : toSea;
                if (tree.Count == 0)
                {
                    break;
                }

                WorldCell cell = tree.Extract(rng);
                cell.Elevation = growLand ? Elevation.Lowland : Elevation.DeepOcean;
                converted.Add(cell);
                RefreshPools(cell);
                budget -= 1;
                needed += growLand ? -1 : 1;
            }

            // Phase 2: spend the rest of the budget on random two-way swaps. Swaps come
            // in pairs — sea to land, then land to sea — so the net effect on the counts
            // is zero and the ratio established by phase 1 is preserved. A pair can only
            // be started while both pools have candidates; if one pool dries up or only a
            // single swap remains in the budget, the pass stops rather than unbalance
            // the counts (a lone leftover swap is left unspent).
            while (budget >= 2 && toLand.Count > 0 && toSea.Count > 0)
            {
                WorldCell cell = toLand.Extract(rng);
                cell.Elevation = Elevation.Lowland;
                converted.Add(cell);
                RefreshPools(cell);
                budget -= 1;

                // The pair was started with at least two swaps in the budget, so the
                // return swap is always affordable; only a dry pool can stop it.
                if (toSea.Count == 0)
                {
                    break;
                }

                WorldCell cell2 = toSea.Extract(rng);
                cell2.Elevation = Elevation.DeepOcean;
                converted.Add(cell2);
                RefreshPools(cell2);
                budget -= 1;
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
