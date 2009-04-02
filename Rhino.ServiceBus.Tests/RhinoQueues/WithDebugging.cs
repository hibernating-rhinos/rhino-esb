using log4net.Appender;
using log4net.Config;
using log4net.Layout;

namespace Rhino.ServiceBus.Tests.RhinoQueues
{
    public class WithDebugging
    {
        static WithDebugging()
        {
            BasicConfigurator.Configure(new DebugAppender
            {
                Layout = new SimpleLayout()
            });
        }
    }
}