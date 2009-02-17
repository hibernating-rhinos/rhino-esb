using System;

namespace Rhino.ServiceBus.Messages
{
    public class Reroute : AdministrativeMessage
    {
        public Uri OriginalEndPoint { get; set; }
        public Uri NewEndPoint { get; set; }
    }
}