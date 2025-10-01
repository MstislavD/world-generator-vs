using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldSimulation.HistorySimulation
{
    class WorldTrait
    {
        public string Name { get; private set; }

        public static WorldTrait PRESAPIENT_CREATURES(HistorySimulator history)
        {
            double probability = 0.05;
            WorldTrait trait = new WorldTrait();
            trait.Name = "Presapient creatures";
            Func<Region, RegionTrait> createTrait = region => RegionTrait.PRESAPIENT_CREATURES(history, region);
            history.RegionTraitsSelection += (s, args) => GenericRegionTraitSelectionAction(args, _regionIsFlatLand, probability, createTrait);
            return trait;
        }

        public static WorldTrait PRESAPIENT_CREATURES_EXACT_NUMBER(HistorySimulator history, int count)
        {
            WorldTrait trait = new WorldTrait();
            trait.Name = $"Presapient creatures (exact number: {count})";

            List<Region> regions = new List<Region>();

            Func<Region, RegionTrait> createTrait = region => RegionTrait.PRESAPIENT_CREATURES(history, region);
            history.RegionTraitsSelection += (s, args) =>
            {
                if (_regionIsFlatLand(args.Region))
                {
                    regions.Add(args.Region);
                }
            };

            history.RegionTraitsSelectionFinish += (s, e) =>
            {
                for (int i = 0; i < count; i++)
                {
                    Region region = history.Random.NextItemExtract(regions);
                    RegionTrait.PRESAPIENT_CREATURES(history, region);
                }
            };

            return trait;
        }

        public static WorldTrait RANDOM_RACE_NAMES(HistorySimulator history)
        {
            WorldTrait trait = new WorldTrait();
            trait.Name = "Random race names";
            history.RaceNameSelection += (s, args) => args.AddName(history.NamingLanguage.GenerateName());
            return trait;
        }

        public static WorldTrait MTG_RACE_NAMES(HistorySimulator history)
        {
            List<string> names = new List<string>() { "Azra", "Centaur", "Cyclops", "Dryad", "Dwarf", "Elf", "Faerie", "Giant", "Gnome", "Goblin",
                "Gorgon", "Halfling", "Human", "Kithkin", "Kobold", "Kor", "Merfolk", "Minotaur", "Moonfolk", "Naga", "Noggle", "Ogre", "Orc", "Satyr", "Siren",
                "Troll", "Vedalken" ,"Viashino", "Ainok", "Amphin", "Aven", "Khenra", "Kitsune", "Leonin","Loxodon", "Nantuko", "Nezumi", "Rhox","Aetherborn",
                "Nightstalker", "Thallid", "Treefolk", "Kappa"};

            WorldTrait trait = new WorldTrait();
            trait.Name = "MTG race names";
            history.RaceNameSelection += (s, args) => args.AddName(history.Random.NextItemExtract(names));
            return trait;
        }

        public static WorldTrait POPS_CAN_MIGRATE(HistorySimulator history)
        {
            WorldTrait trait = new WorldTrait();
            trait.Name = "Populations can migrate";
            history.PopCreated += (s, e) =>
            {
                Population pop = s as Population;
                pop.AssignTrait(PopulationTrait.CAN_MIGRATE(history, pop));
            };
            return trait;
        }

        public static WorldTrait POPS_CAN_GROW(HistorySimulator history)
        {
            WorldTrait trait = new WorldTrait();
            trait.Name = "Populations can grow";
            history.PopCreated += (s, e) =>
            {
                Population pop = s as Population;
                pop.AssignTrait(PopulationTrait.CAN_GROW(history, pop));
            };
            return trait;
        }

        public static WorldTrait PREFERABLE_BELTS(HistorySimulator history)
        {
            WorldTrait trait = new WorldTrait();
            trait.Name = "Races have preferable belts";
            history.RaceCreated += (s, e) =>
            {
                Race race = s as Race;
                if (e.Region.Belt == Belt.Polar)
                    race.AddTag(RacialTrait.Tag.PREFERS_POLAR);
                else if (e.Region.Belt == Belt.Boreal)
                    race.AddTag(RacialTrait.Tag.PREFERS_BOREAL);
                else if (e.Region.Belt == Belt.Temperate)
                    race.AddTag(RacialTrait.Tag.PREFERS_TEMPERATE);
                else if (e.Region.Belt == Belt.Subtropical)
                    race.AddTag(RacialTrait.Tag.PREFERS_SUBTROPICAL);
                else
                    race.AddTag(RacialTrait.Tag.PREFERS_TROPICAL);
            };
            return trait;
        }

        public static WorldTrait MIGRATORY_AND_SEDENTARY(HistorySimulator history)
        {
            double chance = 0.4;
            WorldTrait trait = new WorldTrait();
            trait.Name = "Races can be migratory or sedentary";
            history.RaceCreated += (s, e) =>
            {
                Race race = s as Race;
                if (history.Random.NextDouble() < chance)
                    race.AddTag(history.Random.NextDouble() < 0.5 ? RacialTrait.Tag.SEDENTARY : RacialTrait.Tag.MIGRATORY);
            };
            return trait;
        }

        public static WorldTrait SLOW_AND_FAST_BREEDERS(HistorySimulator history)
        {
            double chance = 0.4;
            WorldTrait trait = new WorldTrait();
            trait.Name = "Races can be slow or fast breeders";
            history.RaceCreated += (s, e) =>
            {
                Race race = s as Race;
                if (history.Random.NextDouble() < chance)
                    race.AddTag(history.Random.NextDouble() < 0.5 ? RacialTrait.Tag.SLOW_BREEDERS : RacialTrait.Tag.FAST_BREEDERS);
            };
            return trait;
        }

        public static WorldTrait PREFERABLE_HUMIDITY(HistorySimulator history)
        {
            WorldTrait trait = new WorldTrait();
            trait.Name = "Races have preferable humidity";
            history.RaceCreated += (s, e) =>
            {
                Race race = s as Race;
                if (e.Region.Humidity == Humidity.Dry)
                    race.AddTag(RacialTrait.Tag.PREFERS_DRY);
                else if (e.Region.Humidity == Humidity.Seasonal)
                    race.AddTag(RacialTrait.Tag.PREFERS_SEASONAL);
                else
                    race.AddTag(RacialTrait.Tag.PREFERS_WET);
            };
            return trait;
        }

        public static WorldTrait SUBRACES(HistorySimulator history)
        {
            WorldTrait trait = new WorldTrait();
            trait.Name = "Subraces";

            history.RaceCreated += (s, e) =>
            {
                Race race = s as Race;
                race.AddTrait(RacialTrait.CAN_HAVE_SUBRACES(history, race));
            };

            return trait;
        }

        static void GenericRegionTraitSelectionAction(RegionTraitSelectionArgs args, Func<Region, bool> reqs, double probability, Func<Region, RegionTrait> createTrait)
        {
            if (reqs(args.Region) && args.History.Random.NextDouble() < probability)
            {
                args.AddTrait(createTrait(args.Region));
            }
        }

        static bool _regionIsFlatLand(Region region) => !region.IsRidge && !region.IsSea;
    }
}
