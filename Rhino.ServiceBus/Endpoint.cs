using System;

namespace Rhino.ServiceBus
{
    public class Endpoint
    {
        private Uri uri;
        public Uri Uri
        {
            get { return uri; }
            set
            {
                if (value.Host.Equals("localhost",StringComparison.InvariantCultureIgnoreCase))
                {
                    uri = new UriBuilder(value)
                    {
                        Host = Environment.MachineName
                    }.Uri;
                }
                else
                {
                    uri = value;
                }
            }
        }

        public override string ToString()
        {
            return string.Format("Uri: {0}", Uri);
        }
    }
}