namespace Rhino.DistributedHashTable.Tests
{
	using System;
	using System.Threading;
	using Castle.Windsor;
	using Castle.Windsor.Configuration.Interpreters;
	using PersistentHashTable;
	using ServiceBus.Impl;
	using ServiceBus;
	using Util;
	using Xunit;

	public class ReplicationFixture : DhtTestBase
	{
		private readonly WindsorContainer secondary;
		private readonly WindsorContainer tertiary;
		private readonly IStartableServiceBus secondaryBus;
		private readonly IStartableServiceBus tertiaryBus;
		private readonly DhtBootStrapper tertiaryBootStrapper;
		private readonly DhtBootStrapper secondaryBootStrapper;
	    private readonly IStartableServiceBus[] buses;
        private readonly DhtBootStrapper[] bootStrappers;

		public ReplicationFixture()
			: base(new XmlInterpreter("Primary.config"))
		{
			secondary = new WindsorContainer(new XmlInterpreter("Secondary.config"));

			secondary.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());

			secondaryBootStrapper = new DhtBootStrapper();
			secondaryBootStrapper.InitializeContainer(secondary);

			secondaryBus = secondary.Resolve<IStartableServiceBus>();
			secondaryBus.Start();

			secondaryBootStrapper.AfterStart();


			tertiary = new WindsorContainer(new XmlInterpreter("Tertiary.config"));
			tertiary.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());

			tertiaryBootStrapper = new DhtBootStrapper();
			tertiaryBootStrapper.InitializeContainer(tertiary);

			tertiaryBus = tertiary.Resolve<IStartableServiceBus>();
			tertiaryBus.Start();

			tertiaryBootStrapper.AfterStart();

		    buses = new[]
		    {
		        bus,
		        secondaryBus,
		        tertiaryBus,
		    };

		    bootStrappers = new[]
		    {
		        bootStrapper,
		        secondaryBootStrapper,
		        tertiaryBootStrapper,
		    };
		}


		[Fact]
		public void When_saving_item_in_one_node_will_be_replicated_to_all_others()
		{
			client.Put(new PutRequest
			{
				Bytes = new byte[] { 1, 2, 3, 4, 5 },
				ParentVersions = new ValueVersion[0],
				Key = "test"
			});


			WaitForValueInEndpoint(client.Nodes[0]);
			WaitForValueInEndpoint(client.Nodes[1]);
			WaitForValueInEndpoint(client.Nodes[2]);
		}

		[Fact]
		public void When_primary_comes_back_up_will_get_values_saved_to_secondary()
		{
			ShutdownPreferredNodeForTestKey();
			//will write to secondary
			client.Put(new PutRequest
			{
				Bytes = new byte[] { 1, 2, 3, 4, 5 },
				ParentVersions = new ValueVersion[0],
				Key = "test"
			});

			BringUpPreferredNodeForTestKey();
			// it may take a few seconds to bring the primary up to date
			var values = new[] { new Value[0], };
			int index = 0;
			while (values[0].Length == 0 && index < 50)
			{
				values = client.Get(new GetRequest
				{
					Key = "test"
				});
				index += 1;
				if (values[0].Length == 0)
					Thread.Sleep(500);
			}
			Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, values[0][0].Data);
		}

		[Fact]
		public void When_primary_and_secondary_comes_back_up_will_get_values_saved_to_tertiary()
		{
			ShutdownPreferredNodeForTestKey();
			ShutdownSecondaryNodeForTestKey();

			//will write to secondary
			client.Put(new PutRequest
			{
				Bytes = new byte[] { 1, 2, 3, 4, 5 },
				ParentVersions = new ValueVersion[0],
				Key = "test"
			});

			BringUpSecondaryNodeForTestKey();

			// it may take a few seconds to bring the secondary up to date
			var values = new[] { new Value[0], };
			int index = 0;
			while (values[0].Length == 0 && index < 50)
			{
				values = client.Get(new GetRequest
				{
					Key = "test"
				});
				index += 1;
				if (values[0].Length == 0)
					Thread.Sleep(500);
			}
			Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, values[0][0].Data);

			BringUpPreferredNodeForTestKey();
			// it may take a few seconds to bring the primary up to date
			values = new[] { new Value[0], };
			index = 0;
			while (values[0].Length == 0 && index < 50)
			{
				values = client.Get(new GetRequest
				{
					Key = "test"
				});
				index += 1;
				if (values[0].Length == 0)
					Thread.Sleep(500);
			}
			Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, values[0][0].Data);
		}

		private void BringUpSecondaryNodeForTestKey()
		{
            var index = (Math.Abs("test".GetHashCode() % 3) + 1) % 3;
            bootStrappers[index].AfterStart();
            buses[index].Start();
		}

		private void ShutdownSecondaryNodeForTestKey()
		{
            var index = (Math.Abs("test".GetHashCode() % 3) + 1) % 3;
		    
			bootStrappers[index].Stop();
			buses[index].Dispose();
		}


		private void BringUpPreferredNodeForTestKey()
		{
            var index = Math.Abs("test".GetHashCode() % 3);
		    bootStrappers[index].AfterStart();
			buses[index].Start();
		}


		[Fact]
		public void Will_failover_if_preferred_node_is_not_available_read()
		{
			client.Put(new PutRequest
			{
				Bytes = new byte[] { 1, 2, 3, 4, 5 },
				ParentVersions = new ValueVersion[0],
				Key = "test"
			});


			// wait for replication
			WaitForValueInEndpoint(client.Nodes[0]);
			WaitForValueInEndpoint(client.Nodes[1]);
			WaitForValueInEndpoint(client.Nodes[2]);

			ShutdownPreferredNodeForTestKey();

			var values = client.Get(new GetRequest { Key = "test" });
			Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, values[0][0].Data);
		}

		private void ShutdownPreferredNodeForTestKey()
		{
		    var index = Math.Abs("test".GetHashCode() % 3);
		    bootStrappers[index].Stop();
			buses[index].Dispose();
		}

		[Fact]
		public void Will_failover_if_preferred_node_is_not_available_write_and_read()
		{
			ShutdownPreferredNodeForTestKey();

			client.Put(new PutRequest
			{
				Bytes = new byte[] { 1, 2, 3, 4, 5 },
				ParentVersions = new ValueVersion[0],
				Key = "test"
			});

			var values = client.Get(new GetRequest { Key = "test" });
			Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, values[0][0].Data);
		}

		[Fact]
		public void When_removing_item_in_one_node_will_be_replicated_to_all_others()
		{
			var results = client.Put(new PutRequest
			{
				Bytes = new byte[] { 1, 2, 3, 4, 5 },
				ParentVersions = new ValueVersion[0],
				Key = "test"
			});

			// wait for replication
			WaitForValueInEndpoint(client.Nodes[0]);
			WaitForValueInEndpoint(client.Nodes[1]);
			WaitForValueInEndpoint(client.Nodes[2]);


			client.Remove(new RemoveRequest
			{
				Key = "test",
				ParentVersions = new[] { results[0].Version }
			});

			WaitForValueRemoval(client.Nodes[0]);
			WaitForValueRemoval(client.Nodes[1]);
			WaitForValueRemoval(client.Nodes[2]);
		}

		private static void WaitForValueRemoval(Node endpoint)
		{
			var index = 0;
			while (index < 10)
			{
				Value[][] values = null;
				ServiceUtil.Execute<IDistributedHashTable>(endpoint.Primary.Sync, table =>
				{
					values = table.Get(new GetRequest
					{
						Key = "test"
					});
				});
				index += 1;
				if (values[0].Length == 0)
					return;
				Thread.Sleep(500);
			}
			Assert.False(true, "could still find value");
		}


		private static void WaitForValueInEndpoint(Node endpoint)
		{
			var index = 0;
			while (index < 10)
			{
				index += 1;
				Value[][] values = null;
				ServiceUtil.Execute<IDistributedHashTable>(endpoint.Primary.Sync, table =>
				{
					values = table.Get(new GetRequest
					{
						Key = "test"
					});
				});
				if (values[0].Length == 0)
				{
					Thread.Sleep(500);
					continue;
				}
				Assert.Equal(values[0][0].Data, new byte[] { 1, 2, 3, 4, 5 });
				return;
			}
			Assert.False(true, "could not find value");
		}

		public override void Dispose()
		{
			base.Dispose();

			secondaryBootStrapper.Dispose();
			tertiaryBootStrapper.Dispose();

			secondaryBus.Dispose();
			tertiaryBus.Dispose();

			secondary.Dispose();
			tertiary.Dispose();
		}
	}
}