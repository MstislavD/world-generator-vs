using System.Collections.Generic;

namespace WorldSimulation.HistorySimulation
{
    public class RegionTrait
    {
        List<string> _names;

        public string Name { get; private set; }        
        public Region Region { get; private set; }

        public IEnumerable<string> Names => _names; 

        public static RegionTrait PRESAPIENT_CREATURES(HistorySimulator history, Region region)
        {
            int maxTurns = 100;
            RegionTrait trait = new RegionTrait();
            trait._names = new List<string>() { history.GetRaceName() };
            trait.Name = $"Presapient creatures ({trait._names[0]})";
            trait.Region = region;
            int turn = history.Random.Next(maxTurns);
            HistoricEvent evolve = HistoricEvent.RACE_EVOLVES(history, trait, turn);
            history.AddEvent(evolve);
            return trait;
        }
    }
}
