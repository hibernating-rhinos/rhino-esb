using System;
using Rhino.ServiceBus.Exceptions;

namespace Rhino.ServiceBus.Msmq
{
	public class QueueCreationModule : IServiceBusAware
	{
		private readonly IQueueStrategy queueStrategy;

		public QueueCreationModule(IQueueStrategy queueStrategy)
		{
			this.queueStrategy = queueStrategy;
		}

		#region IServiceBusAware Members

		public void BusStarting(IServiceBus bus)
		{
			try
			{
				queueStrategy.InitializeQueue(bus.Endpoint, QueueType.Standard);
			}
			catch (Exception e)
			{
				throw new TransportException(
					"Could not open queue: " + bus.Endpoint + Environment.NewLine +
					"Queue path: " + MsmqUtil.GetQueuePath(bus.Endpoint), e);
			}
		}

		public void BusStarted(IServiceBus bus)
		{
		}

		public void BusDisposing(IServiceBus bus)
		{
		}

		public void BusDisposed(IServiceBus bus)
		{
		}

		#endregion
	}
}