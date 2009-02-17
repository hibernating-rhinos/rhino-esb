namespace Rhino.ServiceBus.DistributedHashTableIntegration
{
	public interface ISagaStateMerger<TState>
		where TState : IVersionedSagaState
	{
		TState Merge(TState[] states);
	}
}