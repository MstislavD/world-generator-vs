using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldSim.HistorySimulation
{
    public class Race
    {
        public event EventHandler PopCreated;
        public event EventHandler TagAssigned;
        public event EventHandler TraitAssigned;
        public string Name { get; internal set; }
        public int Seed { get; internal set; }
        public bool RegionIsSuitable(Region region) => !region.IsSea;
        public Race Parent { get; set; }

        HashSet<RacialTrait.Tag> _tags = new HashSet<RacialTrait.Tag>();
        HashSet<RacialTrait> _traits = new HashSet<RacialTrait>();

        public Population CreatePopulation(Region region)
        {
            Population pop = new Population();
            pop.Race = this;
            pop.Move(region);
            PopCreated?.Invoke(pop, new EventArgs());
            return pop;
        }
        public void AddTag(RacialTrait.Tag tag)
        {
            _tags.Add(tag);
            TagAssigned?.Invoke(tag, new EventArgs());
        }

        public void AddTrait(RacialTrait trait)
        {
            _traits.Add(trait);
            TraitAssigned?.Invoke(trait, new EventArgs());
        }
        public bool HasTag(RacialTrait.Tag tag) => _tags.Contains(tag);
        public IEnumerable<RacialTrait.Tag> Tags => _tags;
        public IEnumerable<RacialTrait> Traits => _traits;
        public bool ClimateIsNotOptimal(Region region)
        {
            if (HasTag(RacialTrait.Tag.PREFERS_BOREAL) && region.Belt != Belt.Boreal)
                return true;
            else if (HasTag(RacialTrait.Tag.PREFERS_TEMPERATE) && region.Belt != Belt.Temperate)
                return true;
            else if (HasTag(RacialTrait.Tag.PREFERS_POLAR) && region.Belt != Belt.Polar)
                return true;
            else if (HasTag(RacialTrait.Tag.PREFERS_SUBTROPICAL) && region.Belt != Belt.Subtropical)
                return true;
            else if (HasTag(RacialTrait.Tag.PREFERS_TROPICAL) && region.Belt != Belt.Tropical)
                return true;

            if (HasTag(RacialTrait.Tag.PREFERS_WET) && region.Humidity != Humidity.Wet)
                return true;
            else if (HasTag(RacialTrait.Tag.PREFERS_SEASONAL) && region.Humidity != Humidity.Seasonal)
                return true;
            else if (HasTag(RacialTrait.Tag.PREFERS_DRY) && region.Humidity != Humidity.Dry)
                return true;

            return false;
        }
        public bool Populates(Region region) => region.Pops.Any(p => p.Race == this);
        public override string ToString() => Name;
    }
}
