using Topology;
using Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldSimulation
{
    public class RegionMap
    {
        
        Dictionary<WorldCell, Region> _regionByCell;
        Dictionary<WorldEdge, Region> _regionByEdge;
        public WorldGeneratorLegacy Generator { get; private set; }
        public IEnumerable<Region> Regions => _regionByCell.Values.Union(_regionByEdge.Values);
        public Region GetRegion(Subregion subregion) => subregion.Type == SubregionType.Cell ? GetRegion(subregion.ParentCell) : GetRegion(subregion.ParentEdge);
        public Region GetRegion(WorldCell cell) => _regionByCell[cell];
        public Region GetRegion(WorldEdge edge) => _regionByEdge[edge];
        public bool ContainsEdge(WorldEdge edge) => _regionByEdge.ContainsKey(edge);
        public int CountNonRidge => _regionByCell.Count;
        public RegionMap (WorldGeneratorLegacy generator)
        {
            Generator = generator;

            _regionByCell = new Dictionary<WorldCell, Region>();
            _regionByEdge = new Dictionary<WorldEdge, Region>();

            foreach(Subregion subregion in generator.SubregionGraph.CellSubregions)
            {
                WorldCell cell = subregion.ParentCell;
                if (!_regionByCell.ContainsKey(cell))
                {
                    _regionByCell[cell] = new Region(this, cell);
                }
                Region region = _regionByCell[cell];
                region.AddSubregion(subregion);
                region.Height = generator.GetHeightD(subregion);
                region.Name = generator.NamingLanguage.GenerateName();       
            }

            foreach(Subregion subregion in generator.SubregionGraph.EdgeSubregions)
            {
                WorldEdge edge = subregion.ParentEdge;
                if (!_regionByEdge.ContainsKey(edge))
                {
                    _regionByEdge[edge] = new Region(this, edge);
                }
                Region region = _regionByEdge[edge];
                region.AddSubregion(subregion);
                region.Height = generator.GetHeightD(subregion);
                region.Name = generator.NamingLanguage.GenerateName();
            }
        }

        public void SetBiomes(WorldGeneratorLegacy generator)
        {
            foreach(Region region in Regions)
            {
                if (region.IsSea)
                    region.Biome = region.Belt == Belt.Polar ? Biomes.PolarSea :
                        generator.GetElevation(region.Cell) == Elevation.DeepOcean ? Biomes.DeepOcean : Biomes.ShallowOcean;
                else if (region.IsRidge || region.Biome == Biomes.Mountains)
                    region.Biome = Biomes.Mountains;
                else
                    region.Biome = Biomes.GetBiome(region.Belt, region.Humidity);
            }            
        }
    }
}
