using System;
using System.Collections.Generic;
using System.Messaging;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Msmq;
using Rhino.ServiceBus.Transport;
using Xunit;

namespace Rhino.ServiceBus.Tests.Bugs
{
	public class Wrong_time_on_delay_messages
	{
		public class Is_fixed_for_FlatQueueStrategy : MsmqFlatQueueTestBase
		{
			private readonly OpenedQueue openedQueue;

			public Is_fixed_for_FlatQueueStrategy()
			{
				openedQueue = MsmqUtil.GetQueuePath(testQueueEndPoint).Open();
			}

			[Fact]
			public void When_bitconverter_offset_in_GetTimeoutMessages_is_16()
			{
				var queueStrategy = new FlatQueueStrategy(new EndpointRouter(), testQueueEndPoint.Uri);
				var dateToMatch = DateTime.Now;

				// Create the message
				var message = new Message("Wes");
				// Set the extension
				var bytes = new List<byte>();
				bytes.AddRange(Guid.NewGuid().ToByteArray());
				bytes.AddRange(BitConverter.GetBytes(dateToMatch.ToBinary()));
				message.Extension = bytes.ToArray();

				// Send the message
				openedQueue.OpenSiblngQueue(SubQueue.Timeout, QueueAccessMode.Send).Send(message);

				// Call the GetTimeoutMessages method to verify dates
				var messageList = new List<TimeoutInfo>(queueStrategy.GetTimeoutMessages(openedQueue));

				// Check that the dates are the same
				Assert.Equal(messageList[0].Time, dateToMatch);
			}
		}

		public class Is_fixed_for_SubQueueStrategy : MsmqTestBase
		{
			private readonly OpenedQueue openedQueue;

			public Is_fixed_for_SubQueueStrategy()
			{
				openedQueue = MsmqUtil.GetQueuePath(TestQueueUri).Open();
			}

			[Fact]
			public void When_bitconverter_offset_in_GetTimeoutMessages_is_16()
			{
				var queueStrategy = new SubQueueStrategy();
				queueStrategy.InitializeQueue(TestQueueUri, QueueType.Standard);
				var dateToMatch = DateTime.Now;

				// Create the message
				var message = new Message("Wes");
				// Set the extension
				var bytes = new List<byte>();
				bytes.AddRange(Guid.NewGuid().ToByteArray());
				bytes.AddRange(BitConverter.GetBytes(dateToMatch.ToBinary()));
				message.Extension = bytes.ToArray();

				// Send the message
				openedQueue.Send(message);

				// Move the message to the Timeout Queue
				var msg = openedQueue.Peek(new TimeSpan(30));
				openedQueue.MoveToSubQueue(SubQueue.Timeout, msg);

				// Call the GetTimeoutMessages method to verify dates
				var messageList = new List<TimeoutInfo>(queueStrategy.GetTimeoutMessages(openedQueue));

				// Check that the dates are the same
				Assert.Equal(messageList[0].Time, dateToMatch);
			}
		}
	}
}
