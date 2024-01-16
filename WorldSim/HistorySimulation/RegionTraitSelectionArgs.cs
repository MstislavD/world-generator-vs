using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldSim.HistorySimulation
{
    struct RegionTraitSelectionArgs
    {
        public Region Region;
        public List<RegionTrait> Traits;
        public HistorySimulator History;

        public RegionTraitSelectionArgs(HistorySimulator history, Region region)
        {
            History = history;
            Region = region;
            Traits = new List<RegionTrait>();
        }

        public void AddTrait(RegionTrait trait) => Traits.Add(trait);
    }
}
