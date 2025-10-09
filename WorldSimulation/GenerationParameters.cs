using Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldSimulation
{
    public class GenerationParameters : ParameterSet
    {
        RandomExtension.RandomExt random = new RandomExtension.RandomExt();

        List<Parameter> seedParameters = new List<Parameter>();

        public Parameter DeformationFrequencyMin { get; } = new Parameter("Frequency Min", 2, 1, 500);
        public Parameter DeformationStrengthMax { get; } = new Parameter("Strength Max", 10.0, 0.1, 50);
        public Parameter DeformationFrequencyMax { get; } = new Parameter("Frequency Max", 100, 1, 500);
        public Parameter DeformationStrengthMin { get; } = new Parameter("Strength Min", 1.0, 0.1, 500);
        public Parameter MainSeed { get; }
        public Parameter SubregionSeed { get; }
        public Parameter DeformationSeed { get; }
        public Parameter HeightSeed { get; }
        public Parameter PrecipitationSeed { get; }
        //public Parameter SeaPct { get; } = new Parameter("Sea %", 0.7, 0.3, 0.9);
        public Parameter DeepPct { get; } = new Parameter("Deep Sea %", 0.7, 0.3, 0.9);
        public Parameter RisePct { get; } = new Parameter("Rise Elevation %", 0.3, 0.1, 0.7);
        public Parameter LowerPct { get; } = new Parameter("Lower Elevation %", 0.1, 0.0, 0.5);
        public Parameter IslandPct { get; } = new Parameter("Island %", 0.03, 0.0, 0.1);
        public Parameter RidgePct { get; } = new Parameter("Ridge %", 0.1, 0.03, 0.3);
        public Parameter RidgeClearPct { get; } = new Parameter("Ridge Clear %", 0.03, 0.01, 0.1);
        public Parameter UniformRegionSize { get; } = new Parameter("Uniform Subregion Size", true);
        public Parameter RegionSmoothing { get; } = new Parameter("Region Smoothing", true);
        public Parameter RegionDeformation { get; } = new Parameter("Deform", true);
        public Parameter DeformationFrequencies { get; } = new Parameter("Number of Deformations", 3, 1, 8);
        public Parameter DeformationDetails { get; } = new Parameter("Detailed", true);
        public Parameter DetalizationFactor { get; } = new Parameter("Detalization", 5.0, 1.0, 50.0);
        public Parameter TemperatureSmoothing { get; } = new Parameter("Temp Smoothing", true);
        public Parameter PrecipitationSmoothingSteps { get; } = new Parameter("Precipitation Smoothing Steps", 1, 0, 4);
        public Parameter PrecipitationSmoothingInertia { get; } = new Parameter("Precipitation Smoothing Inertia", 2, 1, 8);
        public Parameter PrecipitationSwapsPct { get; } = new Parameter("Precipiation Swaps %", 0.2, 0, 0.5);
        public Parameter MapScript { get; } = new Parameter("Map Script", "Random", new List<string> { "Random", "One Continent", "Two Continents", "Three Continents" });
        public Parameter LandSize { get; } = new Parameter("Land Size", "Medium", new List<string> { "Tiny", "Small", "Medium", "Large", "Huge", "Colossal" });
        public Parameter Hemispheres { get; } = new Parameter("Hemispheres", "Two Hemishperes", new List<string> { "Two Hemishperes", "North Hemisphere", "South Hemisphere" });
        public Parameter Climate { get; } = new Parameter("Climate", "Balanced", new List<string> { "Balanced", "Dry", "Wet" });
        public Parameter RiverPct { get; } = new Parameter("River %", 0.2, 0.05, 0.5);
        public Parameter TributaryThreshold { get; } = new Parameter("Tributary Threshold", 1, 0.0, 2.2);

        public GenerationParameters(IParameterSupplier supplier) : base(supplier)
        {
            MainSeed = _addSeedParameter("Main Seed");
            SubregionSeed = _addSeedParameter("Subregion Seed");
            DeformationSeed = _addSeedParameter("Deformation Seed");
            HeightSeed = _addSeedParameter("Height Seed");
            PrecipitationSeed = _addSeedParameter("Precipitation Seed");

            //AddParameter(SubregionSeed);
            //AddParameter(DeformationSeed);
            //AddParameter(HeightSeed);
            //AddParameter(PrecipitationSeed);

            AddParameter(MapScript);
            AddParameter(LandSize);
            AddParameter(Hemispheres);
            AddParameter(Climate);
        }

        public GenerationParameters() : base()
        {
            MainSeed = _addSeedParameter("Main Seed");
            SubregionSeed = _addSeedParameter("Subregion Seed");
            DeformationSeed = _addSeedParameter("Deformation Seed");
            HeightSeed = _addSeedParameter("Height Seed");
            PrecipitationSeed = _addSeedParameter("Precipitation Seed");
        }

        public GenerationParameters(string mapScript, string landMass, string hemispheres, string climate) : this()
        {
            UpdateParameter(MapScript, new ParameterValue(mapScript));
            UpdateParameter(LandSize, new ParameterValue(landMass));
            UpdateParameter(Hemispheres, new ParameterValue(hemispheres));
            UpdateParameter(Climate, new ParameterValue(climate));
        }

        public bool IsSeed(Parameter parameter) => seedParameters.Contains(parameter);

        public void RegenerateSeeds()
        {
            foreach(Parameter parameter in seedParameters)
            {
                UpdateParameter(parameter, new ParameterValue(random.Next()));
            }
        }

        public void RegenerateSeeds(int seed)
        {
            Random rnd = new Random(seed);
            foreach (Parameter parameter in seedParameters)
            {
                UpdateParameter(parameter, new ParameterValue(rnd.Next()));
            }
        }

        Parameter _addSeedParameter(string name)
        {
            Parameter seedParameter = new Parameter(name, random.Next(), isSeed: true);
            seedParameters.Add(seedParameter);
            return seedParameter;
        }
    }
}
