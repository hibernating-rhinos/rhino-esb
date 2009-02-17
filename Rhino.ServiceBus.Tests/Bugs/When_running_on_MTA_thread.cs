using System;
using System.Threading;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Xunit;

namespace Rhino.ServiceBus.Tests.Bugs
{
    public class When_running_on_MTA_thread : MsmqTestBase
    {

        private readonly IWindsorContainer container;

        public When_running_on_MTA_thread()
        {
            container = new WindsorContainer(new XmlInterpreter());
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
        }

        [Fact]
        public void Can_start_and_stop_using_implicit_MTA()
        {
            Exception ex = null;
            var thread = new Thread(delegate()
            {
                try
                {
                    var transport = container.Resolve<ITransport>();
                    transport.Start();
                    transport.Dispose();
                }
                catch (Exception e)
                {
                    ex = e;
                }
            });
            thread.Start();
            thread.Join();

            Assert.Null(ex);
        }


        [Fact]
        public void Can_start_and_stop_using_explicit_MTA()
        {
            Exception ex = null;
            var thread = new Thread(delegate()
            {
                try
                {
                    var transport = container.Resolve<ITransport>();
                    transport.Start();
                    transport.Dispose();
                }
                catch (Exception e)
                {
                    ex = e;
                }
            });
            thread.SetApartmentState(ApartmentState.MTA);
            thread.Start();
            thread.Join();

            Assert.Null(ex);
        }
    }
}