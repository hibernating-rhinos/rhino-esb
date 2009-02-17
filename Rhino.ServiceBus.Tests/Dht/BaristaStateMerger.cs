using Rhino.ServiceBus.Sagas;
using System.Linq;

namespace Rhino.ServiceBus.Tests.Dht
{
	using DistributedHashTableIntegration;

	public class BaristaStateMerger : ISagaStateMerger<BaristaState>
    {
        public BaristaState Merge(BaristaState[] states)
        {
            var merged = new BaristaState();
            foreach (var state in states)
            {
                if(state.DrinkIsReady)
                    merged.DrinkIsReady = true;
                if(state.GotPayment)
                    merged.GotPayment = true;
            }
            merged.Drink = states
                .OrderByDescending(x => x.Version)
                .First().Drink;
            return merged;
        }
    }
}