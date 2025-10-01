using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldSimulation.HistorySimulation
{
    class PopulationTrait
    {
        public string Name { get; private set; }
        public static PopulationTrait CAN_MIGRATE(HistorySimulator history, Population pop)
        {
            int maxTurns = 20;
            double factor = 2.0;
            PopulationTrait trait = new PopulationTrait();
            trait.Name = "CAN_MIGRATE";

            if (pop.Race.HasTag(RacialTrait.Tag.SEDENTARY))
                maxTurns = (int)(maxTurns * factor);
            else if (pop.Race.HasTag(RacialTrait.Tag.MIGRATORY))
                maxTurns = (int)(maxTurns / factor);

            Action addEvent = () => throw new Exception();
            addEvent = () =>
            {
                int turn = history.Random.Next(maxTurns);
                HistoricEvent migrateEvent = HistoricEvent.POP_MIGRATES(history, pop, turn);
                history.AddEvent(migrateEvent);
                migrateEvent.OnResolution += (s, e) => addEvent();
            };

            addEvent();

            return trait;
        }
        public static PopulationTrait CAN_GROW(HistorySimulator history, Population pop)
        {
            int maxTurns = 50;
            double factor = 2.0;
            PopulationTrait trait = new PopulationTrait();
            trait.Name = "CAN_GROW";

            if (pop.Race.HasTag(RacialTrait.Tag.SLOW_BREEDERS))
                maxTurns = (int)(maxTurns * factor);
            else if (pop.Race.HasTag(RacialTrait.Tag.FAST_BREEDERS))
                maxTurns = (int)(maxTurns / factor);

            Action addEvent = () => throw new Exception();
            addEvent = () =>
            {
                int turn = history.Random.Next(maxTurns);
                HistoricEvent growthEvent = HistoricEvent.POP_GROWS(pop, turn);
                history.AddEvent(growthEvent);
                growthEvent.OnResolution += (s, e) => addEvent();
            };

            addEvent();

            return trait;
        }
        public override string ToString() => Name;
    }
}
