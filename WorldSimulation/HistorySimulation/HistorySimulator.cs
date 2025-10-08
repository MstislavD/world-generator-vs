using System;
using System.Collections.Generic;
using System.Linq;

namespace WorldSimulation.HistorySimulation
{
    public class HistorySimulator
    {
        WorldGenerator _generator;
        Dictionary<int, HashSet<HistoricEvent>> _eventsByTurn;
        List<Race> _races;

        public List<string> Words = new List<string>() { "White","Black","Red","Green","Blue","Yellow","Brown","Grey","Sun","Moon","Star","Sky","Cloud",
            "Rain","Snow","Forest","Sand","River","Sea","Rock","Hill","Mountain","Cave","High","Low","Pale","Dark", "Light","Chaos","Faerie","Arcane","Small",
            "Wicked","Dusk","Dawn","Night","Death","Free","Tree","Flower","Broken","Exiled","Evil","Celestial","Storm","Air","Water","Earth","Fire","Corrupted"};

        public RandomExtension.RandomExt Random { get; private set; }
        public Language NamingLanguage => _generator.NamingLanguage;
        public int Turn { get; private set; }
        public bool IsFinished { get; private set; } = false;
        public int EventCount { get; private set; }

        public HistorySimulator(int seed, WorldGenerator generator)
        {
            Random = new RandomExtension.RandomExt(seed);
            _generator = generator;
            _eventsByTurn = new Dictionary<int, HashSet<HistoricEvent>>();
            _races = new List<Race>();
        }

        public void Simulate() => Simulate(0);

        public void Simulate(int turns)
        {
            List<WorldTrait> worldTraits =
            [
                WorldTrait.PRESAPIENT_CREATURES(this),
                //WorldTrait.PRESAPIENT_CREATURES_EXACT_NUMBER(this, 1);
                WorldTrait.RANDOM_RACE_NAMES(this),
                //WorldTrait.MTG_RACE_NAMES(this);
                WorldTrait.POPS_CAN_MIGRATE(this),
                WorldTrait.POPS_CAN_GROW(this),
                WorldTrait.PREFERABLE_BELTS(this),
                WorldTrait.PREFERABLE_HUMIDITY(this),
                WorldTrait.MIGRATORY_AND_SEDENTARY(this),
                WorldTrait.SLOW_AND_FAST_BREEDERS(this),
                WorldTrait.SUBRACES(this),
            ];

            foreach (WorldTrait trait in worldTraits)
                _log($"New world trait: {trait.Name}");

            foreach (Region region in Random.Permutation(_generator.RegionMap.Regions.ToList()))
            {
                var args = new RegionTraitSelectionArgs(this, region);
                RegionTraitsSelection?.Invoke(this, args);
                foreach (RegionTrait trait in args.Traits)
                {
                    region.AddTrait(trait);
                    _log($"New trait of region {region}): {trait.Name}");
                }
            }

            RegionTraitsSelectionFinish?.Invoke(this, null);

            for (int i = 0; i < turns; i++)
            {
                _progressTurn();
            }
        }

        private void OnPopCreation(object sender, EventArgs e)
        {
            Population pop = sender as Population;
            _log($"T{Turn}: New {pop.Race} pop created in {pop.Region}");
            pop.TraitAssigned += (s, ee) => _log($"T{Turn}: Trait assigned to {pop.Race} pop in {pop.Region}: {ee.Trait}");
            PopCreated?.Invoke(pop, new EventArgs());
        }

        HistoricEvent _nextEvent()
        {
            int failedEvents = 100000;

            while (_eventsByTurn.Count() > 0)
            {
                if (_eventsByTurn.ContainsKey(Turn) && _eventsByTurn[Turn].Count > 0)
                {
                    HistoricEvent hEvent = _eventsByTurn[Turn].First();
                    _eventsByTurn[Turn].Remove(hEvent);
                    if (hEvent.Resolve())
                    {
                        EventCount += 1;
                        return hEvent;
                    }                        
                    else
                    {
                        failedEvents -= 1;
                        if (failedEvents == 0)
                        {
                            IsFinished = true;
                            return null;
                        }                            
                    }
                }
                else
                {
                    _eventsByTurn.Remove(Turn);
                    Turn += 1;
                }
            }

            IsFinished = true;
            return null;
        }

        public HistoricEvent NextEvents(int events)
        {
            HistoricEvent hEvent = null;
            for (int i = 0; i < events; i++)
                hEvent = _nextEvent();
            return hEvent;
        }

        public HistoricEvent NextTrackedEvent()
        {
            HistoricEvent hEvent = _nextEvent();
            while (hEvent != null && !hEvent.Tracked)
                hEvent = _nextEvent();
            return hEvent;
        }

        internal Race CreateRace(string name, Region region)
        {
            Race race = new Race();
            race.Name = name;
            race.Seed = Random.Next();
            _log($"T{Turn}: New race created: {name}");
            race.TagAssigned += (s, ee) => _log($"T{Turn}: Trait assigned to {race} race: {s}");
            RaceCreated?.Invoke(race, new EventContextArgs() { Region = region });
            race.PopCreated += OnPopCreation;
            _races.Add(race);
            return race;
        }

        public Race CreateRace(string name, Region region, Race parent)
        {
            Race race = CreateRace(name, region);
            race.Parent = parent;
            return race;
        }

        internal void AddEvent(HistoricEvent hEvent)
        {
            int turn = Turn + hEvent.Turn + 1;
            if (!_eventsByTurn.ContainsKey(turn))
                _eventsByTurn[turn] = new HashSet<HistoricEvent>();
            _eventsByTurn[turn].Add(hEvent);
            hEvent.OnResolution += (s, e) => _log($"T{Turn}: {s}");
            if (hEvent.Tracked)
                hEvent.OnResolution += (s, e) => EventLogged?.Invoke(this, hEvent);
        }

        public string GetRaceName()
        {
            SelectItemArgs<string> args = new SelectItemArgs<string>();
            RaceNameSelection?.Invoke(this, args);
            return args.Count > 0 ? args.GetItem(Random) : throw new Exception();
        }

        public IEnumerable<Race> Races => _races;
        public int CountPops(Race race) => Regions.Sum(r => r.PopCount(race));
        public IEnumerable<Region> Regions => _generator.RegionMap.Regions;

        public event EventHandler<string> LogUpdate;
        public event EventHandler<HistoricEvent> EventLogged;

        internal event EventHandler<RegionTraitSelectionArgs> RegionTraitsSelection;
        internal event EventHandler RegionTraitsSelectionFinish;
        internal event EventHandler<SelectItemArgs<string>> RaceNameSelection;
        internal event EventHandler PopCreated;
        internal event EventHandler<EventContextArgs> RaceCreated;

        void _log(string entry) => LogUpdate?.Invoke(this, entry);

        void _progressTurn()
        {
            if (_eventsByTurn.ContainsKey(Turn))
            {
                foreach (HistoricEvent hEvent in _eventsByTurn[Turn])
                {
                    hEvent.Resolve();
                }
            }
            Turn += 1;
        }
    }
}
