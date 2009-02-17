using Rhino.ServiceBus.Sagas;

namespace Rhino.ServiceBus.Tests.Dht
{
	using DistributedHashTableIntegration;
	using PersistentHashTable;

	public class BaristaState : IVersionedSagaState
	{
		public bool DrinkIsReady { get; set; }

		public bool GotPayment { get; set; }

		public string Drink { get; set; }

		#region IVersionedSagaState Members

		public ValueVersion Version { get; set; }

		public ValueVersion[] ParentVersions { get; set; }

		#endregion
	}
}