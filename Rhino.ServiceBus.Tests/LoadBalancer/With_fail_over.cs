using System;
using System.Messaging;
using System.Threading;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.LoadBalancer;
using Rhino.ServiceBus.Messages;
using Rhino.ServiceBus.Msmq;
using Xunit;
using MessageType = Rhino.ServiceBus.Transport.MessageType;
using System.Linq;

namespace Rhino.ServiceBus.Tests.LoadBalancer
{
    public class With_fail_over : LoadBalancingTestBase
    {
        private readonly IWindsorContainer container;

        public With_fail_over()
        {
            var interpreter = new XmlInterpreter(@"LoadBalancer\BusWithLoadBalancer.config");
            container = new WindsorContainer(interpreter);
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());

            container.Register(
                Component.For<MsmqLoadBalancer>()
                    .DependsOn(new
                    {
                        threadCount = 1,
                        endpoint = new Uri(loadBalancerQueue),
                        secondaryLoadBalancer = TestQueueUri2.Uri
                    })
                );
        }

        [Fact]
        public void when_start_load_balancer_that_has_secondary_will_start_sending_heartbeats_to_secondary()
        {
            using (var loadBalancer = container.Resolve<MsmqLoadBalancer>())
            {
                loadBalancer.Start();

                Message peek = testQueue2.Peek(TimeSpan.FromSeconds(30));
                object[] msgs = container.Resolve<IMessageSerializer>().Deserialize(peek.BodyStream);

                Assert.IsType<Heartbeat>(msgs[0]);
                var beat = (Heartbeat)msgs[0];
                Assert.Equal(loadBalancer.Endpoint.Uri, beat.From);
            }
        }


        [Fact]
        public void When_Primary_loadBalacer_recieve_workers_it_sends_them_to_secondary_loadBalancer()
        {
            using (var loadBalancer = container.Resolve<MsmqLoadBalancer>())
            using (var bus = container.Resolve<IStartableServiceBus>())
            {
                var wait = new ManualResetEvent(false);
                int timesCalled = 0;
                loadBalancer.SentNewWorkerPersisted += () =>
                {
                    timesCalled += 1;
                    //we get three ReadyToWork mesages, one from the bus itself
                    //the other two from the mesages that we are explicitly sending.
                    if (timesCalled == 3)
                        wait.Set();
                };


                loadBalancer.Start();

                bus.Start();

                bus.Send(loadBalancer.Endpoint,
                         new ReadyToWork
                         {
                             Endpoint = new Uri("msmq://app1/work1")
                         });

                bus.Send(loadBalancer.Endpoint,
                         new ReadyToWork
                         {
                             Endpoint = new Uri("msmq://app1/work1")
                         });

                bus.Send(loadBalancer.Endpoint,
                         new ReadyToWork
                         {
                             Endpoint = new Uri("msmq://app2/work1")
                         });


                wait.WaitOne(TimeSpan.FromSeconds(30), false);

                var messageSerializer = container.Resolve<IMessageSerializer>();

                using (var workers = new MessageQueue(testQueuePath2, QueueAccessMode.SendAndReceive))
                {
                    int busUri = 0;
                    int app1 = 0;
                    int app2 = 0;

                    foreach (Message msg in workers.GetAllMessages())
                    {
                        object msgFromQueue = messageSerializer.Deserialize(msg.BodyStream)[0];

                        var newWorkerPersisted = msgFromQueue as NewWorkerPersisted;
                        if (newWorkerPersisted == null)
                            continue;

                        if (newWorkerPersisted.Endpoint.ToString() == "msmq://app1/work1")
                            app1 += 1;
                        else if (newWorkerPersisted.Endpoint.ToString() == "msmq://app2/work1")
                            app2 += 1;
                        else if (newWorkerPersisted.Endpoint.ToString().ToLower().Replace(Environment.MachineName.ToLower(), "localhost") ==
                            bus.Endpoint.Uri.ToString().ToLower())
                            busUri += 1;
                    }

                    Assert.Equal(app1, 1);
                    Assert.Equal(app2, 1);
                }
            }
        }


        [Fact]
        public void When_Primary_loadBalacer_recieve_endPoints_it_sends_them_to_secondary_loadBalancer()
        {
            using (var loadBalancer = container.Resolve<MsmqLoadBalancer>())
            {
                var wait = new ManualResetEvent(false);
                int timesCalled = 0;
                loadBalancer.SentNewEndpointPersisted += () =>
                {
                    timesCalled += 1;
                    if (timesCalled == 2)
                        wait.Set();
                };

                loadBalancer.Start();


				using (var loadBalancerMsmqQueue = MsmqUtil.GetQueuePath(loadBalancer.Endpoint).Open(QueueAccessMode.SendAndReceive))
                {
                    var queuePath = MsmqUtil.GetQueuePath(TestQueueUri2);
                    loadBalancerMsmqQueue.Send(new Message
                    {
						ResponseQueue = queuePath.Open().ToResponseQueue(),
                        Body = "a"
                    });

                    loadBalancerMsmqQueue.Send(new Message
                    {
						ResponseQueue = queuePath.Open().ToResponseQueue(),
                        Body = "a"
                    });

                    queuePath = MsmqUtil.GetQueuePath(TransactionalTestQueueUri);
                    loadBalancerMsmqQueue.Send(new Message
                    {
						ResponseQueue = queuePath.Open().ToResponseQueue(),
                        Body = "a"
                    });
                }


                wait.WaitOne(TimeSpan.FromSeconds(30), false);

                var messageSerializer = container.Resolve<IMessageSerializer>();

                using (var workers = new MessageQueue(testQueuePath2, QueueAccessMode.SendAndReceive))
                {
                    int work1 = 0;
                    int work2 = 0;

                    foreach (Message msg in workers.GetAllMessages())
                    {
                        object msgFromQueue = messageSerializer.Deserialize(msg.BodyStream)[0];

                        var newEndpointPersisted = msgFromQueue as NewEndpointPersisted;
                        if (newEndpointPersisted == null)
                            continue;

                        var endpoint = newEndpointPersisted.PersistedEndpoint;
                        if (endpoint == TestQueueUri2.Uri)
                            work1 += 1;
                        else if (endpoint == TransactionalTestQueueUri.Uri)
                            work2 += 1;
                    }

                    Assert.Equal(work1, 1);
                    Assert.Equal(work2, 1);
                }
            }
        }

        [Fact]
        public void When_Primary_loadBalacer_gets_SendAllKnownWorkersAndEndpoints_will_send_them()
        {
            using (var loadBalancer = container.Resolve<MsmqLoadBalancer>())
            {
                loadBalancer.KnownWorkers.Add(new Uri("msmq://test1/bar"));
                loadBalancer.KnownWorkers.Add(new Uri("msmq://test2/bar"));

                loadBalancer.KnownEndpoints.Add(new Uri("msmq://test3/foo"));
                loadBalancer.KnownEndpoints.Add(new Uri("msmq://test4/foo"));

                testQueue2.Purge();// removing existing ones.

                SendMessageToBalancer(testQueue2, new QueryForAllKnownWorkersAndEndpoints());

                var workers = loadBalancer.KnownWorkers.GetValues().ToList();
                var endpoints = loadBalancer.KnownEndpoints.GetValues().ToList();
                var messageSerializer = container.Resolve<IMessageSerializer>();
               
                while (workers.Count == 0 && endpoints.Count == 0)
                {
                    var transportMesage = testQueue2.Receive(TimeSpan.FromSeconds(30));
                    var msgs = messageSerializer.Deserialize(transportMesage.BodyStream);
                    foreach (var msg in msgs)
                    {
                        if (msg is Heartbeat)
                            continue;

                        if(msg is NewEndpointPersisted)
                        {
                            endpoints.Remove(((NewEndpointPersisted) msg).PersistedEndpoint);
                        }
                        if(msg is NewWorkerPersisted)
                        {
                            workers.Remove(((NewWorkerPersisted) msg).Endpoint);
                        }
                    }
                }
            }
        }

        private void SendMessageToBalancer(
            MessageQueue reply,
            LoadBalancerMessage msg)
        {
            var messageSerializer = container.Resolve<IMessageSerializer>();
            var message = new Message
            {
                ResponseQueue = reply,
                AppSpecific = (int)MessageType.LoadBalancerMessageMarker
            };
            messageSerializer.Serialize(new[] { msg }, message.BodyStream);
            using (var q = new MessageQueue(loadBalancerQueuePath))
            {
                q.Send(message);
            }
        }
    }
}