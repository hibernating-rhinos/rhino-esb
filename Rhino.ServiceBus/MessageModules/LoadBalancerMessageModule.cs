using System;
using log4net;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Messages;

namespace Rhino.ServiceBus.MessageModules
{
    public class LoadBalancerMessageModule : IMessageModule
    {
        private ITransport theTransport;
        private readonly ILog logger = LogManager.GetLogger(typeof(LoadBalancerMessageModule));

        private readonly Uri loadBalancerEndpoint;
        private readonly IEndpointRouter endpointRouter;

        public LoadBalancerMessageModule(Uri loadBalancerEndpoint, IEndpointRouter endpointRouter)
        {
            this.loadBalancerEndpoint = loadBalancerEndpoint;
            this.endpointRouter = endpointRouter;
        }

        public void Init(ITransport transport)
        {
            transport.MessageProcessingCompleted += Transport_OnMessageProcessingCompleted;
            theTransport = transport;
            theTransport.MessageArrived += TheTransport_OnMessageArrived;
            theTransport.Started += TellLoadBalancerThatWeAreReadyToWorkForAllThreads;
            logger.DebugFormat("This node {0} is load balanced by {1}",
                               transport.Endpoint,
                               loadBalancerEndpoint);
        }

        private bool TheTransport_OnMessageArrived(CurrentMessageInformation message)
        {
            var acceptingWork = message.Message as AcceptingWork;

            if (acceptingWork != null)
            {
                TellLoadBalancerThatWeAreReadyToWorkForAllThreads();
                return true;
            }

            return false;
        }

        private void TellLoadBalancerThatWeAreReadyToWorkForAllThreads()
        {
            var readyToWork = new object[theTransport.ThreadCount];
            for (var i = 0; i < theTransport.ThreadCount; i++)
            {
                readyToWork[i] = new ReadyToWork
                {
                    Endpoint = theTransport.Endpoint.Uri
                };
            }
            var endpoint = endpointRouter.GetRoutedEndpoint(loadBalancerEndpoint);
            logger.DebugFormat("Telling load balancer {0} that we {1} are ready to do work in {2} threads",
                               endpoint,
                               theTransport.Endpoint,
                               readyToWork.Length);
            theTransport.Send(endpoint, readyToWork);
        }

        private void Transport_OnMessageProcessingCompleted(CurrentMessageInformation t1, Exception t2)
        {
            TellLoadBalancerThatWeAreReadyForWork();
        }

        private void TellLoadBalancerThatWeAreReadyForWork()
        {
            var endpoint = endpointRouter.GetRoutedEndpoint(loadBalancerEndpoint);
            logger.DebugFormat("Telling load balancer {0} that we {1} are ready for more work",
                               endpoint,
                               theTransport.Endpoint);
            theTransport.Send(endpoint, new ReadyToWork
            {
                Endpoint = theTransport.Endpoint.Uri
            });
        }

        public void Stop(ITransport transport)
        {
            transport.MessageProcessingCompleted -= Transport_OnMessageProcessingCompleted;
            theTransport = null;
        }
    }
}