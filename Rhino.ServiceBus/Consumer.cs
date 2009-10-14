namespace Rhino.ServiceBus
{
	public class Consumer<T>
	{
		public interface SkipAutomaticSubscription : ConsumerOf<T>
		{
		}
	}
}