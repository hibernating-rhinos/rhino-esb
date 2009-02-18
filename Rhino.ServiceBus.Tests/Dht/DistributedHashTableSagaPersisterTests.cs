using System;
using System.ComponentModel.Design;
using System.IO;
using System.Threading;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Sagas;
using Xunit;
using System.Linq;

namespace Rhino.ServiceBus.Tests.Dht
{
	using DistributedHashTable;
	using DistributedHashTableIntegration;
	using PersistentHashTable;
	using Rhino.ServiceBus.Hosting;

	public class DistributedHashTableSagaPersisterTests : MsmqTestBase,
        OccasionalConsumerOf<DrinkReady>
    {
        private readonly IWindsorContainer container;
		private readonly RemoteAppDomainHost host;

		public DistributedHashTableSagaPersisterTests()
        {
            Delete("cache.esent");
            BaristaSaga.WaitToCreateConflicts = new ManualResetEvent(true);
            BaristaSaga.FinishedConsumingMessage = new ManualResetEvent(false);
            container = new WindsorContainer(new XmlInterpreter());
            container.Kernel.AddFacility("rhino.esb", 
                new RhinoServiceBusFacility()
                );
			container.Kernel.AddFacility("dht.saga", new DhtClientSagaFacility());
            container.AddComponent<BaristaSaga>();
            container.AddComponent<ISagaStateMerger<BaristaState>, BaristaStateMerger>();

			host = new RemoteAppDomainHost(
				typeof (DhtBootStrapper).Assembly.Location,
				Path.Combine(Path.GetDirectoryName(typeof (DhtBootStrapper).Assembly.Location), "DhtService.config")
				);
			host.Start();
        }

        private void Delete(string database)
        {
            if(Directory.Exists(database))
                Directory.Delete(database, true);
        }

        [Fact]
        public void Will_put_saga_state_in_dht()
        {
            var guid = Guid.NewGuid();
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                bus.Send(bus.Endpoint, new PrepareDrink
                {
                    CorrelationId = guid,
                    CustomerName = "ayende",
                    DrinkName = "Coffee"
                });

                BaristaSaga.FinishedConsumingMessage.WaitOne(TimeSpan.FromSeconds(30), false);
            }

            var distributedHashTable = container.Resolve<IDistributedHashTableClient>();
            Value[] values;

            do
            {
                Thread.Sleep(100);
                values = distributedHashTable.Get(new[]
                {
                    new GetRequest
                    {
                        Key = typeof(BaristaSaga) + "-" + guid
                    },
                }).First();
            } while (values.Length==0);

            var messageSerializer = container.Resolve<IMessageSerializer>();
            var state = (BaristaState)messageSerializer.Deserialize(new MemoryStream(values[0].Data))[0];

            Assert.True(state.DrinkIsReady);
            Assert.False(state.GotPayment);
            Assert.Equal("Coffee", state.Drink);
        }

        [Fact]
        public void When_saga_complete_will_remove_from_dht()
        {
            var guid = Guid.NewGuid();

            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                bus.Send(bus.Endpoint, new PrepareDrink
                {
                    CorrelationId = guid,
                    CustomerName = "ayende",
                    DrinkName = "Coffee"
                });

                BaristaSaga.FinishedConsumingMessage.WaitOne(TimeSpan.FromSeconds(30), false);
                BaristaSaga.FinishedConsumingMessage.Reset();

                using(bus.AddInstanceSubscription(this))
                {
                    bus.Send(bus.Endpoint, new PaymentComplete
                    {
                        CorrelationId = guid,
                    });
                    BaristaSaga.FinishedConsumingMessage.WaitOne(TimeSpan.FromSeconds(30), false);
                }
            }

			var distributedHashTable = container.Resolve<IDistributedHashTableClient>();
           
            Value[] values;
            do
            {
                Thread.Sleep(100);
                values = distributedHashTable.Get(new[]
                {
                    new GetRequest
                    {
                        Key = typeof(BaristaSaga) +"-" +guid
                    },
                }).First();
            } while (values.Length != 0);

            Assert.Equal(0, values.Length);
        }

        [Fact(Skip = "This test needs to be rewritten to make it understandable, I can't figure out what is going on, and I wrote it!")]
        public void When_dht_contains_conflicts_when_saga_is_completed_will_call_saga_again()
        {
            var guid = Guid.NewGuid();

            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                bus.Start();

                bus.Send(bus.Endpoint, new PrepareDrink
                {
                    CorrelationId = guid,
                    CustomerName = "ayende",
                    DrinkName = "Coffee"
                });

                BaristaSaga.FinishedConsumingMessage.WaitOne(TimeSpan.FromSeconds(30), false);
                BaristaSaga.FinishedConsumingMessage.Reset();

                BaristaSaga.WaitToCreateConflicts = new ManualResetEvent(false);

                using (bus.AddInstanceSubscription(this))
                {
                    bus.Send(bus.Endpoint, new PaymentComplete
                    {
                        CorrelationId = guid,
                    });
                    BaristaSaga.FinishedConsumingMessage.WaitOne(TimeSpan.FromSeconds(30), false);

                    BaristaSaga.FinishedConsumingMessage.Reset();
                    
                    var sagaPersister = container.Resolve<ISagaPersister<BaristaSaga>>();
                    var saga = new BaristaSaga(bus)
                    {
                        Id = guid,
                        State = {Drink = "foo"}
                    };
                    sagaPersister.Save(saga);

                    BaristaSaga.WaitToCreateConflicts.Set();
                }
                BaristaSaga.FinishedConsumingMessage.WaitOne(TimeSpan.FromSeconds(30), false);
            }


            Assert.Equal("foo", BaristaSaga.DrinkName);
        }

        public void Consume(DrinkReady message)
        {
            
        }

        public override void  Dispose()
        {
            base.Dispose();
            container.Dispose();
			host.Close();
        }
    }
}