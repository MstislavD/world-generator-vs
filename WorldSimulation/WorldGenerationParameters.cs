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

        /// <summary>Target fraction of base-layer edges that become ridges.</summary>
        public ParameterRange<double> RidgePct { get; } = new("Ridges %", 0.2, 0, 1);

        public WorldGenerationParameters()
        {
            Seed = new ParameterSeed("Seed", random.Next());

            Add(Seed, false);
            Add(SeaPct, false);
            Add(SeaToLand);
            Add(SwapPct);
            Add(RidgePct, false);
        }
    }
}
