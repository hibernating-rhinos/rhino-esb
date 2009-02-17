namespace Rhino.DistributedHashTable.Util
{
	using System;
	using System.ServiceModel;

	public static class ServiceUtil
	{
		public static void Execute<TSrv>(Uri uri, Action<TSrv> action)
		{
			bool success = false;
		
			var channel = ChannelFactory<TSrv>.CreateChannel(
				Binding.DhtDefault, 
				new EndpointAddress(uri));
			try
			{
				var communicationObject = (ICommunicationObject)channel;
				action(channel);
				communicationObject.Close();
				success = true;
			}
			finally
			{
				if (success == false)
					((ICommunicationObject)channel).Abort();
			}
		}
	}
}