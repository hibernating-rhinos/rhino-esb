using System;

namespace Rhino.ServiceBus
{
    public class Endpoint
    {
        public Uri Uri { get; set; }

        public override string ToString()
        {
            return string.Format("Uri: {0}", Uri);
        }
    }
}