using System;
using System.IO;
using Castle.MicroKernel;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Messages;
using Rhino.ServiceBus.RhinoQueues;
using Rhino.ServiceBus.Serializers;
using Xunit;
using System.Linq;

namespace Rhino.ServiceBus.Tests.RhinoQueues
{
    public class PhtSubscriptionStorageFixture : IDisposable, OccasionalConsumerOf<string>
    {
        private readonly PhtSubscriptionStorage storage;

        public PhtSubscriptionStorageFixture()
        {
            if (Directory.Exists("test.esent"))
                Directory.Delete("test.esent", true);

            storage = new PhtSubscriptionStorage("test.esent",
                                                 new XmlMessageSerializer(new DefaultReflection(), new DefaultKernel()),
                                                 new DefaultReflection());
            storage.Initialize();
        }

        [Fact]
        public void WillDetectDuplicateSubscription()
        {
            Assert.True(storage.ConsumeAddSubscription(new AddSubscription
            {
                Endpoint = new Endpoint
                {
                    Uri = new Uri("rhino.queues://foo/bar")
                },
                Type = "System.String"
            }));
            Assert.False(storage.ConsumeAddSubscription(new AddSubscription
            {
                Endpoint = new Endpoint
                {
                    Uri = new Uri("rhino.queues://foo/bar")
                },
                Type = "System.String"
            }));
        }

        [Fact]
        public void CanQueryForExistingSubscriptions()
        {
            storage.ConsumeAddSubscription(new AddSubscription
            {
                Endpoint = new Endpoint
                {
                    Uri = new Uri("rhino.queues://foo/bar" )
                },
                Type = "System.String"
            });
            storage.ConsumeAddSubscription(new AddSubscription
            {
                Endpoint = new Endpoint
                {
                    Uri = new Uri("rhino.queues://foo/baz" )
                },
                Type = "System.String"
            });

            Assert.Equal(
                new []
                {
                    new Uri("rhino.queues://foo/bar"),
                    new Uri("rhino.queues://foo/baz"),
                },
                storage.GetSubscriptionsFor(typeof(string)).ToArray()
                );
        }


        [Fact]
        public void CanRemoveSubscription()
        {
            storage.ConsumeAddSubscription(new AddSubscription
            {
                Endpoint = new Endpoint
                {
                    Uri = new Uri("rhino.queues://foo/bar" )
                },
                Type = "System.String"
            });

            storage.ConsumeRemoveSubscription(new RemoveSubscription()
            {
                Endpoint = new Endpoint
                {
                    Uri = new Uri("rhino.queues://foo/bar" )
                },
                Type = "System.String"
            });

            Assert.Equal(
                new Uri[] { },
                storage.GetSubscriptionsFor(typeof(string)).ToArray()
                );
        }

        [Fact]
        public void CanAddLocalSubscripton()
        {
            storage.AddLocalInstanceSubscription(this);
            
            Assert.Equal(
                this,
                storage.GetInstanceSubscriptions(typeof(string))[0]
                );
        }

        [Fact]
        public void CanRemoveLocalSubscripton()
        {
            storage.AddLocalInstanceSubscription(this);

            Assert.Equal(
                this,
                storage.GetInstanceSubscriptions(typeof(string))[0]
                );

            storage.RemoveLocalInstanceSubscription(this);

            Assert.Empty(
                storage.GetInstanceSubscriptions(typeof(string))
                );
        }

        [Fact]
        public void CanAddRemoteInstanceSubscription()
        {
            storage.ConsumeAddInstanceSubscription(
                new AddInstanceSubscription
                {
                    Endpoint = new Uri("rhino.queues://localhost/foobar").ToString(),
                    InstanceSubscriptionKey = Guid.NewGuid(),
                    Type = typeof(string).ToString()
                });

            Assert.Equal(
                new Uri("rhino.queues://localhost/foobar"),
                storage.GetSubscriptionsFor(typeof(string)).Single()
                );
        }

        [Fact]
        public void CanRemoveRemoteInstanceSubscription()
        {
            var guid = Guid.NewGuid();
            storage.ConsumeAddInstanceSubscription(
                new AddInstanceSubscription
                {
                    Endpoint = new Uri("rhino.queues://localhost/foobar").ToString(),
                    InstanceSubscriptionKey = guid,
                    Type = typeof(string).ToString()
                });

            storage.ConsumeRemoveInstanceSubscription(
                new RemoveInstanceSubscription()
                {
                    Endpoint = new Uri("rhino.queues://localhost/foobar").ToString(),
                    InstanceSubscriptionKey = guid,
                    Type = typeof(string).ToString()
                });

            Assert.Empty(
                storage.GetSubscriptionsFor(typeof(string))
                );
        }

        public void Dispose()
        {
            storage.Dispose();
        }

        public void Consume(string message)
        {
            
        }
    }
}