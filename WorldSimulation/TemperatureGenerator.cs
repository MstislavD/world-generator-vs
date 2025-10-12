using Topology;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldSimulation
{
    class TemperatureGenerator
    {
        public static void Generate(WorldGenerator generator, Random random)
        {
            double height = generator.SubregionGraph.Height;
            double equator = height / 2;

            foreach (Region region in generator.RegionMap.Regions)
            {
                double distanceFromPole = 1 - Math.Abs(region.Center.Y - equator) / equator;
                if (generator.Parameters.Hemispheres == Hemispheres.North_hemisphere)
                    distanceFromPole = region.Center.Y / height;
                else if (generator.Parameters.Hemispheres == Hemispheres.South_hemisphere)
                    distanceFromPole = 1 - region.Center.Y / height;
                
                region.Temperature = distanceFromPole;
            }

            if (generator.Parameters.TemperatureSmoothing)
            {
                Dictionary<Region, double> temperatureByRegion = generator.RegionMap.Regions.ToDictionary(r => r, r => r.Temperature);
                foreach (Region region in generator.RegionMap.Regions)
                { 
                    region.Temperature = region.CellNeighbors.Append(region).Average(r => temperatureByRegion[r]) + random.NextDouble() * 0.01;
                }

                List<Region> regions = generator.RegionMap.Regions.OrderBy(r => r.Temperature).ToList();
                for (int i = 0; i < regions.Count; i++)
                {
                    regions[i].Temperature = (double)i / regions.Count;
                    regions[i].Belt = GetBelt(regions[i].Temperature);
                }
            }
        }

        public static Belt GetBelt(double temperature)
        {
                if (temperature < 0.1)
                    return Belt.Polar;
                else if (temperature < 0.35)
                    return Belt.Boreal;
                else if (temperature < 0.6)
                    return Belt.Temperate;
                else if (temperature < 0.85)
                    return Belt.Subtropical;
                else
                    return Belt.Tropical;
        }
    }
}
