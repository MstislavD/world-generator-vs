using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldSimulation.HistorySimulation
{
    struct PopulationEventArgs
    {
        public Region PreviousRegion;
        public PopulationTrait Trait;
        public PopulationEventArgs(Region prevRegion)
        {
            Trait = null;
            PreviousRegion = prevRegion;
        }
        public PopulationEventArgs(PopulationTrait trait)
        {
            Trait = trait;
            PreviousRegion = null;
        }
    }
}
