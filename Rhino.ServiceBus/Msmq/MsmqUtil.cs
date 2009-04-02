using System;
using System.Messaging;
using System.Net;
using System.Security.Principal;
using Rhino.ServiceBus.Exceptions;

namespace Rhino.ServiceBus.Msmq
{
    using System.Text.RegularExpressions;

    public class MsmqUtil
    {
        private static Regex guidRegEx = 
            new Regex(@"^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$",
                RegexOptions.Compiled);



        public static QueueInfo GetQueuePath(Endpoint endpoint)
        {
            var uri = endpoint.Uri;
            if (uri.AbsolutePath.IndexOf("/", 1) >= 0)
            {
                throw new InvalidOperationException(
                    "Invalid enpoint url : " + uri + Environment.NewLine +
                    "Queue Endpoints can't have a child folders (including 'public')" + Environment.NewLine +
                    "Good: 'msmq://machinename/queue_name'." + Environment.NewLine +
                    " Bad: msmq://machinename/round_file/queue_name"
                    );
            }

            string hostName = uri.Host;
            string queuePathWithFlatSubQueue =
                uri.AbsolutePath.Substring(1);
            if (string.IsNullOrEmpty(uri.Fragment) == false && uri.Fragment.Length > 1)
                queuePathWithFlatSubQueue += uri.Fragment;
            if (string.Compare(hostName, ".") == 0 ||
                string.Compare(hostName, Environment.MachineName, true) == 0 ||
                string.Compare(hostName, "localhost", true) == 0)
            {
                return new QueueInfo
                {
                    IsLocal = true,
                    QueuePath = Environment.MachineName + @"\private$\" + queuePathWithFlatSubQueue,
                    QueueUri = uri
                };
            }

            IPAddress address;
            if (IPAddress.TryParse(hostName, out address))
            {
                return new QueueInfo
                {
                    IsLocal = false,
                    QueuePath = "FormatName:DIRECT=TCP:" + hostName + @"\private$\" + queuePathWithFlatSubQueue,
                    QueueUri = uri
                };
            }
            if (guidRegEx.IsMatch(hostName))
            {
                return new QueueInfo
                {
                    IsLocal = false,
                    QueuePath = "FormatName:PRIVATE=" + hostName + @"\" + queuePathWithFlatSubQueue,
                    QueueUri = uri
                };  
            }
            return new QueueInfo
            {
                IsLocal = false,
                QueuePath = "FormatName:DIRECT=OS:" + hostName + @"\private$\" + queuePathWithFlatSubQueue,
                QueueUri = uri
            };
        }

        static readonly Regex queuePathDirect = new Regex(@"FormatName:DIRECT=(?<transport>\w+):(?<machineName>[\w\d-_.#$;]+)\\private\$\\(?<queueName>[\w\d-_.#$;]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex queuePath = new Regex(@"FormatName:PRIVATE=(?<machineGuid>[\w\d-_.#$;]+)\\(?<queueNumber>[\w\d-_.#$;]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Uri GetQueueUri(MessageQueue queue)
        {
            if (queue == null)
                return null;
            var directMatch = queuePathDirect.Match(queue.Path);
            if (directMatch.Success)
            {
                return new Uri("msmq://" + directMatch.Groups["machineName"] + "/" +
                               directMatch.Groups["queueName"]);
            }
            var match = queuePath.Match(queue.Path);
            if (match.Success)
            {
                return new Uri("msmq://" + match.Groups["machineGuid"] + "/" +
                               match.Groups["queueNumber"]);
            }
            throw new ArgumentException("Could not understand queue format: " + queue.Path);
        }

        public static MessageQueue OpenOrCreateQueue(string newQueuePath, QueueAccessMode accessMode)
        {
            try
            {
                bool exists;
                try
                {
                    exists = MessageQueue.Exists(newQueuePath);
                }
                catch (InvalidOperationException)// probably a queue on a remote machine
                {
                    return new MessageQueue(newQueuePath);
                }
                if (exists == false)
                {
                    try
                    {
                        CreateQueue(newQueuePath);
                    }
                    catch (Exception e)
                    {
                        throw new TransportException("Queue " + newQueuePath + " doesn't exists and we failed to create it", e);
                    }
                }

                return new MessageQueue(newQueuePath, accessMode);
            }
            catch (Exception e)
            {
                throw new MessagePublicationException("Could not open queue (" + newQueuePath + ")", e);
            }
        }

        public static MessageQueue CreateQueue(string newQueuePath)
        {
            var queue = MessageQueue.Create(newQueuePath, true);
            var administratorsGroupName = new SecurityIdentifier("S-1-5-32-544")
                                                .Translate(typeof(NTAccount))
                                                .ToString();
            queue.SetPermissions(administratorsGroupName, MessageQueueAccessRights.FullControl, AccessControlEntryType.Allow);
            return queue;
        }
        
    }
}
