namespace Rhino.DistributedHashTable
{
	using System;
	using System.Collections.Generic;
	using System.ServiceModel;
	using Castle.MicroKernel;
	using ServiceBus.Hosting;

	public class DhtBootStrapper : AbstractBootStrapper
	{
		private readonly List<ServiceHost> hosts = new List<ServiceHost>();
		private ServiceHost metaDataHost;

		protected override void ConfigureContainer()
		{
			base.ConfigureContainer();
			container.AddComponent<IDistributedHashTable, DistributedHashTable>("dht");
			container.AddComponent<IDistributedHashTableMetaDataProvider, DistributedHashTableMetaDataProvider>("dht.metadata");
		}

		public override void AfterStart()
		{
			StartMetadataProviderIfNeeded();

			StartDhtNodes();
		}

		private void StartDhtNodes()
		{
			var distributedHashTables = container.ResolveAll<IDistributedHashTable>();

			foreach (var distributedHashTable in distributedHashTables)
			{
				var host = new ServiceHost(distributedHashTable);
				host.AddServiceEndpoint(typeof(IDistributedHashTable), Binding.DhtDefault, distributedHashTable.Url);
				host.Open();
				hosts.Add(host);
			}
		}

		private void StartMetadataProviderIfNeeded()
		{
			var handler = container.Kernel.GetHandler("dht.metadata");
			if (handler != null && handler.CurrentState != HandlerState.Valid) 
				return;

			var metaDataProvider = container.Resolve<IDistributedHashTableMetaDataProvider>("dht.metadata");

			metaDataHost = new ServiceHost(metaDataProvider);
			metaDataHost.AddServiceEndpoint(typeof(IDistributedHashTableMetaDataProvider), Binding.DhtDefault,
			                                metaDataProvider.Url);
			metaDataHost.Open();
		}

		public override void Dispose()
		{
			Stop();
			base.Dispose();
		}

		public void Stop()
		{
			foreach (var host in hosts)
			{
				host.Close(TimeSpan.Zero);
			}
			hosts.Clear();
			if (metaDataHost != null)
				metaDataHost.Close(TimeSpan.Zero);
		}
	}
}