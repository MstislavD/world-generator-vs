using Topology;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldSimulation
{
    public class LandmassData
    {
        static int _distance = 3;

        List<Landmass> _landmasses;

        public LandmassData(WorldGeneratorLegacy generator, Random random)
        {
            _landmasses = new List<Landmass>();
            Func<Region, bool> validRegion = r => r.IsFlat && r.Biome != Biomes.Mountains;

            foreach (Region region in generator.RegionMap.Regions)
            {
                if (region.Landmass == null && validRegion(region))
                {
                    Landmass landmass = new Landmass(generator, random.Next());
                    _landmasses.Add(landmass);
                    foreach (Region landmassRegion in Node.Flood(region, validRegion))
                    {
                        landmassRegion.Landmass = landmass;
                        landmass.AddRegion(landmassRegion);
                    }
                }
            }

            foreach (Landmass landmass in _landmasses)
            {
                HashSet<Landmass> neighbors = new HashSet<Landmass>();
                HashSet<Region> mass = new HashSet<Region>(landmass.Regions);
                HashSet<Region> layer = new HashSet<Region>(mass);
                for (int i = 0; i < _distance; i++)
                {
                    layer = new HashSet<Region>(layer.SelectMany(r => r.CellNeighbors.Where(n => !mass.Contains(n) && n.Biome != Biomes.Mountains)));
                    neighbors.UnionWith(layer.Select(r => r.Landmass));
                    mass.UnionWith(layer);
                }

                foreach (Landmass landMassNeighbor in neighbors.Where(n => n != null && n != landmass))
                    landmass.AddNeighbor(landMassNeighbor);
            }
        }

        public IEnumerable<Landmass> Landmasses => _landmasses;
    }
}
