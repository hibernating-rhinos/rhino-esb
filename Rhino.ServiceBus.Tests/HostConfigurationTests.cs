using System.Transactions;
using Rhino.ServiceBus.Hosting;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class HostConfigurationTests
    {
        [Fact]
        public void isolationlevel_is_translated_to_configuration()
        {
            var hostConfiguration = new HostConfiguration();
            hostConfiguration.IsolationLevel(IsolationLevel.ReadCommitted);
            var config = hostConfiguration.ToBusConfiguration();
            
            Assert.Equal("ReadCommitted", config.Bus.QueueIsolationLevel);
        }
    }
}