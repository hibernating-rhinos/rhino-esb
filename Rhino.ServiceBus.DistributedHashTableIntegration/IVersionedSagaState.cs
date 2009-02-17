namespace Rhino.ServiceBus.DistributedHashTableIntegration
{
	using PersistentHashTable;

	public interface IVersionedSagaState
	{
		ValueVersion Version { get; set; }
		ValueVersion[] ParentVersions { get; set; }
	}
}