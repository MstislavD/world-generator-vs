using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldSimulation.HistorySimulation
{
    public struct EventResolutionArgs
    {
        public bool Success;

        public EventResolutionArgs(bool success)
        {
            Success = success;
        }
    }

    public class HistoricEvent
    {
        public event EventHandler<EventResolutionArgs> OnResolution;
        public string Info { get; private set; }
        public int Turn { get; internal set; }
        public Func<bool> Resolve { get; private set; }
        public Region Origin { get; private set; }
        public Region Destination { get; private set; }
        public bool Tracked { get; private set; }

        public static HistoricEvent RACE_EVOLVES(HistorySimulator history, RegionTrait trait, int turn)
        {
            HistoricEvent hEvent = new HistoricEvent();
            hEvent.Info = $"{trait.Names.First()} race evolves in {trait.Region}";
            hEvent.Turn = turn;
            hEvent.Origin = trait.Region;

            hEvent.Resolve = () =>
            {
                hEvent.OnResolution?.Invoke(hEvent, new EventResolutionArgs(true));
                Race race = history.CreateRace(trait.Names.First(), trait.Region);
                race.CreatePopulation(trait.Region);        
                return true;
            };

            return hEvent;
        }

        public static HistoricEvent SUBRACE_EVOLVES(HistorySimulator history, Race race, int turn)
        {
            HistoricEvent hEvent = new HistoricEvent();
            hEvent.Info = $"New {race} subrace evolves";
            hEvent.Turn = turn;
            hEvent.Tracked = true;

            hEvent.Resolve = () =>
            {
                List<Region> nonidealRegions = new List<Region>();

                foreach (Region region in history.Regions.Where(race.Populates))
                {
                    if (region.Size > region.PopCount() && race.ClimateIsNotOptimal(region))
                        nonidealRegions.Add(region);

                    nonidealRegions.AddRange(region.FlatNeighbors.Where(r => r.Size > r.PopCount()).Where(race.ClimateIsNotOptimal));
                }

                if (nonidealRegions.Count == 0)
                {
                    hEvent.OnResolution?.Invoke(hEvent, new EventResolutionArgs(false));
                    return false;
                }

                Region subraceHome = history.Random.NextItem(nonidealRegions);

                Race ancestor = race;
                while (ancestor.Parent != null)
                    ancestor = ancestor.Parent;

                string prefix = history.Random.NextItem(history.Words);

                string subraceName = prefix + " " + ancestor.Name;
                Race subrace = history.CreateRace(subraceName, subraceHome, race);
                subrace.CreatePopulation(subraceHome);

                hEvent.OnResolution?.Invoke(hEvent, new EventResolutionArgs(true));
                return true;
            };

            return hEvent;
        }

        public static HistoricEvent POP_MIGRATES(HistorySimulator history, Population pop, int turn)
        {
            HistoricEvent hEvent = new HistoricEvent();
            hEvent.Info = $"{pop.Race} pop migrates from {pop.Region}";
            hEvent.Turn = turn;
            hEvent.Origin = pop.Region;

            Belt belt = Belt.NA;
            if (pop.Race.HasTag(RacialTrait.Tag.PREFERS_POLAR))
                belt = Belt.Polar;
            else if (pop.Race.HasTag(RacialTrait.Tag.PREFERS_BOREAL))
                belt = Belt.Boreal;
            else if (pop.Race.HasTag(RacialTrait.Tag.PREFERS_TEMPERATE))
                belt = Belt.Temperate;
            else if (pop.Race.HasTag(RacialTrait.Tag.PREFERS_SUBTROPICAL))
                belt = Belt.Subtropical;
            else if (pop.Race.HasTag(RacialTrait.Tag.PREFERS_TROPICAL))
                belt = Belt.Tropical;

            Humidity humidity = Humidity.NA;
            if (pop.Race.HasTag(RacialTrait.Tag.PREFERS_DRY))
                humidity = Humidity.Dry;
            else if (pop.Race.HasTag(RacialTrait.Tag.PREFERS_SEASONAL))
                humidity = Humidity.Seasonal;
            if (pop.Race.HasTag(RacialTrait.Tag.PREFERS_WET))
                humidity = Humidity.Wet;

            Func<Region, bool> regionIsSuitable = region =>
            {
                if (!pop.Race.RegionIsSuitable(region))
                    return false;
                if (region.Size <= region.PopCount())
                    return false;

                int humDelta = humidity == Humidity.NA ? 0 : Math.Abs(region.Humidity - humidity);
                int beltDelta = belt == Belt.NA ? 0 : Math.Abs(region.Belt - belt);
                if (beltDelta + humDelta > 1)
                    return false;
               
                return true;
            };

            hEvent.Resolve = () =>
            {
                if (hEvent.Origin != pop.Region)
                    throw new Exception("Pop region has changed before migration");
                List<Region> suitableDestinations = hEvent.Origin.CellNeighbors.Where(regionIsSuitable).ToList();
                if (suitableDestinations.Count > 0)
                {
                    hEvent.Destination = history.Random.NextItem(suitableDestinations);
                    hEvent.Info = $"{pop.Race} pop migrates from {hEvent.Origin} to {hEvent.Destination}";                    
                    pop.Move(hEvent.Destination);
                    hEvent.OnResolution?.Invoke(hEvent, new EventResolutionArgs(true));
                    return true;
                }
                else
                {
                    hEvent.Info = $"{pop.Race} pop can't migrate from {hEvent.Origin}";
                    hEvent.OnResolution?.Invoke(hEvent, new EventResolutionArgs(false));
                    return false;
                }               
            };

            return hEvent;
        }

        public static HistoricEvent POP_GROWS(Population pop, int turn)
        {
            HistoricEvent hEvent = new HistoricEvent();
            hEvent.Turn = turn;

            hEvent.Resolve = () =>
            {
                if (pop.Region.Size > pop.Region.PopCount())
                {
                    hEvent.Info = $"{pop.Race} pop grows in {pop.Region}";
                    hEvent.Origin = pop.Region;
                    hEvent.OnResolution?.Invoke(hEvent, new EventResolutionArgs(true));
                    pop.Race.CreatePopulation(pop.Region);
                    return true;
                }
                else
                {
                    hEvent.Info = $"{pop.Race} pop can't grow in {pop.Region}";
                    hEvent.OnResolution?.Invoke(hEvent, new EventResolutionArgs(false));
                    return false;
                }
            };

            return hEvent;
        }

        public override string ToString() => Info;
    }
}
