using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Utilities;

namespace WorldSimulation
{
    public enum MapScript { Random, One_continent, Two_continents, Three_continents }
    public enum LandSize { Tiny, Small, Medium, Large, Huge, Colossal }
    public enum Hemispheres { Two_hemispheres, North_hemisphere, South_hemisphere }
    public enum Climate { Balanced, Dry, Wet }
    public class GenerationParameters : ParameterList
    {
        RandomExt random = new RandomExt();

        List<ParameterSeed> seedParameters = new List<ParameterSeed>();

        public ParameterRange<int> DeformationFrequencyMin { get; } = new("Frequency Min", 2, 1, 500);
        public ParameterRange<double> DeformationStrengthMax { get; } = new("Strength Max", 10.0, 0.1, 50);
        public ParameterRange<int> DeformationFrequencyMax { get; } = new("Frequency Max", 100, 1, 500);
        public ParameterRange<double> DeformationStrengthMin { get; } = new("Strength Min", 1.0, 0.1, 500);
        public ParameterSeed MainSeed { get; }
        public ParameterSeed SubregionSeed { get; }
        public ParameterSeed DeformationSeed { get; }
        public ParameterSeed HeightSeed { get; }
        public ParameterSeed PrecipitationSeed { get; }
        public ParameterRange<double> DeepPct { get; } = new("Deep Sea %", 0.7, 0.3, 0.9);
        public ParameterRange<double> RisePct { get; } = new("Rise Elevation %", 0.3, 0.1, 0.7);
        public ParameterRange<double> LowerPct { get; } = new("Lower Elevation %", 0.1, 0.0, 0.5);
        public ParameterRange<double> IslandPct { get; } = new("Island %", 0.03, 0.0, 0.1);
        public ParameterRange<double> RidgePct { get; } = new("Ridge %", 0.1, 0.03, 0.3);
        public ParameterRange<double> RidgeClearPct { get; } = new("Ridge Clear %", 0.03, 0.01, 0.1);
        public Parameter<bool> UniformRegionSize { get; } = new("Uniform Subregion Size", true);
        public Parameter<bool> RegionSmoothing { get; } = new("Region Smoothing", true);
        public Parameter<bool> RegionDeformation { get; } = new("Deform", true);
        public ParameterRange<int> DeformationFrequencies { get; } = new("Number of Deformations", 3, 1, 8);
        public Parameter<bool> DeformationDetails { get; } = new("Detailed", true);
        public ParameterRange<double> DetalizationFactor { get; } = new("Detalization", 5.0, 1.0, 50.0);
        public Parameter<bool> TemperatureSmoothing { get; } = new("Temp Smoothing", true);
        public ParameterRange<int> PrecipitationSmoothingSteps { get; } = new("Precipitation Smoothing Steps", 1, 0, 4);
        public ParameterRange<int> PrecipitationSmoothingInertia { get; } = new("Precipitation Smoothing Inertia", 2, 1, 8);
        public ParameterRange<double> PrecipitationSwapsPct { get; } = new("Precipiation Swaps %", 0.2, 0, 0.5);
        public ParameterEnum<MapScript> MapScript { get; } = new("Map Script", WorldSimulation.MapScript.Random);
        public ParameterEnum<LandSize> LandSize { get; } = new("Land Size", WorldSimulation.LandSize.Medium);
        public ParameterEnum<Hemispheres> Hemispheres { get; } = new("Hemispheres", WorldSimulation.Hemispheres.Two_hemispheres);
        public ParameterEnum<Climate> Climate { get; } = new ("Climate", WorldSimulation.Climate.Balanced);
        public ParameterRange<double> RiverPct { get; } = new("River %", 0.2, 0.05, 0.5);
        public ParameterRange<double> TributaryThreshold { get; } = new("Tributary Threshold", 1, 0.0, 2.2);

        public GenerationParameters() : base()
        {
            MainSeed = new("Main Seed", random.Next());
            SubregionSeed = new("Subregion Seed", random.Next());
            DeformationSeed = new("Deformation Seed", random.Next());
            HeightSeed = new("Height Seed", random.Next());
            PrecipitationSeed = new("Precipitation Seed", random.Next());

            seedParameters.AddRange([MainSeed, SubregionSeed, DeformationSeed, HeightSeed, PrecipitationSeed]);

            Add(MapScript);
            Add(LandSize);
            Add(Hemispheres);
            Add(Climate);
        }

        public void RegenerateSeeds()
        {
            foreach (Parameter parameter in seedParameters)
                parameter.Update(this, random.Next());
        }

        public void RegenerateSeeds(int seed)
        {
            random = new RandomExt(seed);
            RegenerateSeeds();
        }
    }
}
