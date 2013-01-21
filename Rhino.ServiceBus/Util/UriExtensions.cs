using System;
using Rhino.ServiceBus.Transport;
using System.Linq;
using System.Collections.Generic;

namespace Rhino.ServiceBus.Util
{
    public static class UriExtensions
    {
        public static Uri AddSubQueue(this Uri self, SubQueue subQueue)
        {
            Uri subQueueUrl;
            if (self.AbsolutePath.EndsWith("/"))
                subQueueUrl = new Uri(self + SubQueue.Discarded.ToString());
            else
                subQueueUrl = new Uri(self + "/" + SubQueue.Discarded);
            return subQueueUrl;
        }

        public static string GetQueueName(this Uri self)
        {
            return self.AbsolutePath.Substring(1).Split('/').First();
        }

        private static HashSet<string> localhosts = new HashSet<string>(new[]{"localhost","127.0.0.1"}, StringComparer.OrdinalIgnoreCase);

        public static Uri NormalizeLocalhost(this Uri uri)
        {
            if (localhosts.Contains(uri.Host))
            {
                return new UriBuilder(uri){ Host = Environment.MachineName }.Uri;
            }
            return uri;
        }
    }
}