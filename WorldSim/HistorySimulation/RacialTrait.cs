using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldSim.HistorySimulation
{
    public class RacialTrait
    {
        public string Name { get; private set; }

        public enum Tag
        {
            PREFERS_POLAR, PREFERS_BOREAL, PREFERS_TEMPERATE, PREFERS_SUBTROPICAL, PREFERS_TROPICAL, PREFERS_DRY, PREFERS_SEASONAL, PREFERS_WET,
            SEDENTARY, MIGRATORY, SLOW_BREEDERS, FAST_BREEDERS
        }

        public static RacialTrait CAN_HAVE_SUBRACES(HistorySimulator history, Race race)
        {
            int maxTurns = 2000;
            RacialTrait trait = new RacialTrait();
            trait.Name = "Can have subraces";

            Action addEvent = () => throw new Exception();
            addEvent = () =>
            {
                int turn = history.Random.Next(maxTurns);
                HistoricEvent evolveSubrace = HistoricEvent.SUBRACE_EVOLVES(history, race, turn);
                history.AddEvent(evolveSubrace);
                evolveSubrace.OnResolution += (s, e) => addEvent();
            };
            addEvent();

            return trait;
        }
    }
}
