using System.Collections.Generic;
using System.Linq;

namespace WorldSimulation
{
    public static class Biomes
    {
        public static Biome WetTundra = new Biome("Tundra", Belt.Polar, Humidity.Wet);
        public static Biome Tundra = new Biome("Tundra", Belt.Polar, Humidity.Seasonal);
        public static Biome DryTundra = new Biome("Tundra", Belt.Polar, Humidity.Dry);
        public static Biome WetTaiga = new Biome("Wet Taiga", Belt.Boreal, Humidity.Wet);
        public static Biome Taiga = new Biome("Taiga", Belt.Boreal, Humidity.Seasonal);
        public static Biome DryTaiga = new Biome("Dry Taiga", Belt.Boreal, Humidity.Dry);
        public static Biome MixedForest = new Biome("Mixed Forest", Belt.Temperate, Humidity.Wet);
        public static Biome ForestSteppe = new Biome("Forest Steppe", Belt.Temperate, Humidity.Seasonal);
        public static Biome TemperateSteppe = new Biome("Steppe", Belt.Temperate, Humidity.Dry);
        public static Biome BroadleafForest = new Biome("Broadleaf Forest", Belt.Subtropical, Humidity.Wet);
        public static Biome SubtropicalSteppe = new Biome("Steppe", Belt.Subtropical, Humidity.Seasonal);
        public static Biome SubtropicalDesert = new Biome("Desert", Belt.Subtropical, Humidity.Dry);
        public static Biome Rainforest = new Biome("Rainforest", Belt.Tropical, Humidity.Wet);
        public static Biome Savanna = new Biome("Savanna", Belt.Tropical, Humidity.Seasonal);
        public static Biome TropicalDesert = new Biome("Desert", Belt.Tropical, Humidity.Dry);
        public static Biome ShallowOcean = new Biome("Shallow Ocean", Belt.NA, Humidity.NA);
        public static Biome DeepOcean = new Biome("Deep Ocean", Belt.NA, Humidity.NA);
        public static Biome Mountains = new Biome("Mountains", Belt.NA, Humidity.NA);
        public static Biome PolarSea = new Biome("Ocean", Belt.Polar, Humidity.NA);

        static List<Biome> _biomes = new List<Biome>() { WetTundra, Tundra, DryTundra, WetTaiga, Taiga, DryTaiga, MixedForest, ForestSteppe, TemperateSteppe,
        BroadleafForest, SubtropicalSteppe, SubtropicalDesert, Rainforest, Savanna, TropicalDesert, ShallowOcean, DeepOcean, PolarSea, Mountains};

        public static Biome GetBiome(Belt belt, Humidity humidity) => _biomes.First(b => b.Belt == belt && b.Humidity == humidity);

    }
}
