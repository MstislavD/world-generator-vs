using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldSim.HistorySimulation
{
    public class Population
    {
        HashSet<PopulationTrait> _traits;

        public Population()
        {
            _traits = new HashSet<PopulationTrait>();
        }

        internal event EventHandler<PopulationEventArgs> PopMove;
        internal event EventHandler<PopulationEventArgs> TraitAssigned;
        public Race Race { get; set; }
        public Region Region { get; set; }
        public void Move(Region destination)
        {
            Region origin = Region;
            origin?.RemovePop(this);
            destination.AddPop(this);
            Region = destination;
            PopMove?.Invoke(this, new PopulationEventArgs(origin));
        }
        internal void AssignTrait(PopulationTrait trait)
        {
            TraitAssigned?.Invoke(this, new PopulationEventArgs(trait));
        }
    }
}
