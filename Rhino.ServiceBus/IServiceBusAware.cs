namespace Rhino.ServiceBus
{
	public interface IServiceBusAware
	{
		void BusStarting(IServiceBus bus);
		void BusStarted(IServiceBus bus);
		void BusDisposing(IServiceBus bus);
		void BusDisposed(IServiceBus bus);
	}
}