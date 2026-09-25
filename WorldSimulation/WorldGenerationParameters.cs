using Utilities;

namespace WorldSimulation
{
    /// <summary>
    /// Generation parameters of the new world generator, mirroring the role of
    /// GenerationParameters in the legacy generator: owns the seed and the user-tunable
    /// generation options, and registers them with a UI panel.
    /// </summary>
    public class WorldGenerationParameters : ParameterList
    {
        RandomExt random = new RandomExt();

        public ParameterSeed Seed { get; }
        public ParameterRange<double> SeaPct { get; } = new("Sea %", 0.7, 0.1, 0.95);
        public Parameter<bool> SeaToLand { get; } = new("Sea to land", true);

        /// <summary>Fraction of the level's tiles swapped between sea and land (both directions combined).</summary>
        public ParameterRange<double> SwapPct { get; } = new("Swaps %", 0.025, 0, 1);

        public WorldGenerationParameters()
        {
            Seed = new ParameterSeed("Seed", random.Next());

            // SeaPct is kept for programmatic use but not shown in the UI for now
            Add(SeaToLand);
            Add(SwapPct);
        }

        /// <summary>Rolls a fresh random seed.</summary>
        public void RegenerateSeeds()
        {
            Seed.Update(this, random.Next());
        }

        /// <summary>Sets the main seed; per-stage sub-seeds (if any) will be derived from it.</summary>
        public void RegenerateSeeds(int seed)
        {
            random = new RandomExt(seed);
            Seed.Update(this, seed);
        }
    }
}
