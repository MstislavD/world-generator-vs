using Topology;
using Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldSimulation
{
    class PrecipitationGenerator
    {
        static bool _printLog = false;

        public static void Generate(WorldGeneratorLegacy generator)
        {
            RandomExt random = new RandomExt(generator.Parameters.PrecipitationSeed);
            RegionMap map = generator.RegionMap;

            List<Region> dryPool = map.Regions.Where(r => r.IsFlat).ToList();
            List<Region> wetPool = dryPool.ToList();

            foreach (Region region in dryPool)
                region.Humidity = Humidity.Seasonal;

            int count = dryPool.Count / 3;
            if (generator.Parameters.Climate != Climate.Balanced)
                count = dryPool.Count / 9;

            int drySteps = 1;
            if (generator.Parameters.Climate == Climate.Dry)
                drySteps = 4;
            else if (generator.Parameters.Climate == Climate.Wet)
                drySteps = 2;

            int wetSteps = 1;
            if (generator.Parameters.Climate == Climate.Wet)
                wetSteps = 4;
            else if (generator.Parameters.Climate == Climate.Dry)
                wetSteps = 2;

            while (count > 0 && dryPool.Count > 0 && wetPool.Count > 0)
            {
                count -= 1;

                for (int i = 0; i < drySteps; i++)
                {
                    _makeDry(random, dryPool);
                }

                for (int i = 0; i < wetSteps; i++)
                {
                    _makeWet(random, wetPool);
                }
            }

            if (_printLog)
            {
                Console.WriteLine($"Dry: {map.Regions.Count(r => r.IsFlat && r.Humidity == Humidity.Dry)}, Pool: {dryPool.Count}");
                Console.WriteLine($"Wet: {map.Regions.Count(r => r.IsFlat && r.Humidity == Humidity.Wet)}, Pool: {wetPool.Count}");
                Console.WriteLine($"Seasonal: {map.Regions.Count(r => r.IsFlat && r.Humidity == Humidity.Seasonal)}");
            }

            map.SetBiomes(generator);
        }

        private static void _makeDry(RandomExt random, List<Region> dryPool)
        {
            Region dryRegion = random.NextItemExtract(dryPool);
            while (dryRegion.FlatNeighbors.Append(dryRegion).Any(r => r.Humidity == Humidity.Wet) && dryPool.Count > 0)
                dryRegion = random.NextItemExtract(dryPool);

            if (!dryRegion.FlatNeighbors.Append(dryRegion).Any(r => r.Humidity == Humidity.Wet))
                dryRegion.Humidity = Humidity.Dry;
        }

        private static void _makeWet(RandomExt random, List<Region> wetPool)
        {
            Region wetRegion = random.NextItemExtract(wetPool);
            while (wetRegion.FlatNeighbors.Append(wetRegion).Any(r => r.Humidity == Humidity.Dry) && wetPool.Count > 0)
                wetRegion = random.NextItemExtract(wetPool);

            if (!wetRegion.FlatNeighbors.Append(wetRegion).Any(r => r.Humidity == Humidity.Dry))
                wetRegion.Humidity = Humidity.Wet;
        }
    }
}
