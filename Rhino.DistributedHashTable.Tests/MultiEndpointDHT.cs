namespace Rhino.DistributedHashTable.Tests
{
	using System;
	using System.Linq;
	using Castle.Windsor.Configuration.Interpreters;
	using PersistentHashTable;
	using Util;
	using Xunit;

	public class MultiEndpointDHT : DhtTestBase
	{
		public MultiEndpointDHT()
			: base(new XmlInterpreter("MultipleEndpoints.config"))
		{
		}

		[Fact]
		public void Can_queue_meta_data_service_and_get_network_description()
		{
			var nodes = new Node[0];
			ServiceUtil.Execute<IDistributedHashTableMetaDataProvider>(metaDataUrl, provider =>
			{
				nodes = provider.GetNetworkNodes();
			});
			Assert.Equal(3, nodes.Length);

			Assert.Equal(new Uri("net.tcp://localhost:8129/dht"), nodes[0].Primary.Sync);
			Assert.Equal(new Uri("net.tcp://localhost:8121/dht"), nodes[1].Primary.Sync);
			Assert.Equal(new Uri("net.tcp://localhost:8122/dht"), nodes[2].Primary.Sync);

			Assert.Equal(new Uri("msmq://localhost/dht_test.replication"), nodes[0].Primary.Async);
			Assert.Equal(new Uri("msmq://localhost/dht_test.replication2"), nodes[1].Primary.Async);
			Assert.Equal(new Uri("msmq://localhost/dht_test.replication3"), nodes[2].Primary.Async);

			Assert.Equal(new Uri("net.tcp://localhost:8121/dht"), nodes[0].Secondary.Sync);
			Assert.Equal(new Uri("net.tcp://localhost:8122/dht"), nodes[1].Secondary.Sync);
			Assert.Equal(new Uri("net.tcp://localhost:8129/dht"), nodes[2].Secondary.Sync);

			Assert.Equal(new Uri("msmq://localhost/dht_test.replication2"), nodes[0].Secondary.Async);
			Assert.Equal(new Uri("msmq://localhost/dht_test.replication3"), nodes[1].Secondary.Async);
			Assert.Equal(new Uri("msmq://localhost/dht_test.replication"), nodes[2].Secondary.Async);

			Assert.Equal(new Uri("net.tcp://localhost:8122/dht"), nodes[0].Tertiary.Sync);
			Assert.Equal(new Uri("net.tcp://localhost:8129/dht"), nodes[1].Tertiary.Sync);
			Assert.Equal(new Uri("net.tcp://localhost:8121/dht"), nodes[2].Tertiary.Sync);

			Assert.Equal(new Uri("msmq://localhost/dht_test.replication3"), nodes[0].Tertiary.Async);
			Assert.Equal(new Uri("msmq://localhost/dht_test.replication"), nodes[1].Tertiary.Async);
			Assert.Equal(new Uri("msmq://localhost/dht_test.replication2"), nodes[2].Tertiary.Async);
		}

		[Fact]
		public void Can_add_and_recieve_items_from_multiple_endpoints()
		{
			var versions = client.Put(new[]
		    {
		        new PutRequest
		        {
		            Key = "test74", 
		            Bytes = new byte[] {74},
                    ParentVersions = new ValueVersion[0]
		        },
		        new PutRequest
		        {
		            Key = "test75", 
		            Bytes = new byte[] {75},
                    ParentVersions = new ValueVersion[0]
		        },
		        new PutRequest
		        {
		            Key = "test77", 
		            Bytes = new byte[] {77},
                    ParentVersions = new ValueVersion[0]
		        },
		    });

			Assert.Equal(new[] { 1, 1, 1 }, versions.Select(x => x.Version.Number).ToArray());

			var values = client.Get(new[]
		    {
		        new GetRequest
		        {
		            Key = "test74",
		        },
		        new GetRequest
		        {
		            Key = "test75",
		        },
		        new GetRequest
		        {
		            Key = "test77",
		        },
		    });
			Assert.Equal(3, values.Length);
			Assert.Equal(new byte[] { 74 }, values[0][0].Data);
			Assert.Equal(new byte[] { 75 }, values[1][0].Data);
			Assert.Equal(new byte[] { 77 }, values[2][0].Data);
		}

		[Fact]
		public void Can_add_and_remove_items_from_multiple_endpoints()
		{
			var versions = client.Put(new[]
		    {
		        new PutRequest
		        {
		            Key = "test74", 
		            Bytes = new byte[] {74},
					ParentVersions = new ValueVersion[0]
		        },
		        new PutRequest
		        {
		            Key = "test75", 
		            Bytes = new byte[] {75},
					ParentVersions = new ValueVersion[0]
		        },
		        new PutRequest
		        {
		            Key = "test77", 
		            Bytes = new byte[] {77},
					ParentVersions = new ValueVersion[0]
		        },
		    });

			Assert.Equal(new[] { 1, 1, 1 }, versions.Select(e => e.Version.Number).ToArray());

			var removed = client.Remove(new[]
		    {
		        new RemoveRequest
		        {
		            Key = "test74",
		            ParentVersions = new []{versions[0].Version}
		        },
		        new RemoveRequest
		        {
		            Key = "test75",
		            ParentVersions = new []{versions[1].Version}
		        },
		        new RemoveRequest
		        {
		            Key = "test77",
		            ParentVersions = new []{versions[2].Version}
		        },
		    });

			Assert.Equal(new[] { true, true, true }, removed);

			var values = client.Get(new[]
		    {
		        new GetRequest
		        {
		            Key = "test74",
		        },
		        new GetRequest
		        {
		            Key = "test75",
		        },
		        new GetRequest
		        {
		            Key = "test77",
		        },
		    });
			Assert.Equal(3, values.Length);
			Assert.Equal(0, values[0].Length);
			Assert.Equal(0, values[1].Length);
			Assert.Equal(0, values[2].Length);
		}
	}
}