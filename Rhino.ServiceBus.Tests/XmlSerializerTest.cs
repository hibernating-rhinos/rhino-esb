using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using Castle.MicroKernel;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Serializers;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class XmlSerializerTest
    {
        private readonly Order sample = new Order
        {
            Url = new Uri("msmq://www.ayende.com/"),
            At = DateTime.Today,
            Count = 5,
            OrderId = new Guid("1909994f-8173-452c-a651-14725bb09cb6"),
            OrderLines = new[]
            {
                new OrderLine
                {
                    Product = "milk",
                    Fubar = new List<int> {1, 2, 3}
                },
                new OrderLine
                {
                    Product = "butter",
                    Fubar = new List<int> {4, 5, 6}
                }
            },
            TimeToDelivery = TimeSpan.FromDays(1),
        };

        [Fact]
        public void Can_serialize_and_deserialize_primitive()
        {
            long ticks = DateTime.Now.Ticks;
            var serializer = new XmlMessageSerializer(new DefaultReflection(), new DefaultKernel());
            var stream = new MemoryStream();
            serializer.Serialize(new object[] {ticks}, stream);
            stream.Position = 0;
            var actual = (long) serializer.Deserialize(stream)[0];
            Assert.Equal(ticks, actual);
        }

		[Fact]
		public void Can_serialize_and_deserialize_byte_array()
		{
			var serializer = new XmlMessageSerializer(new DefaultReflection(), new DefaultKernel());
			var stream = new MemoryStream();
			serializer.Serialize(new object[] { new byte[]{1,2,3,4} }, stream);
			stream.Position = 0;
			var actual = (byte[])serializer.Deserialize(stream)[0];
			Assert.Equal(new byte[]{1,2,3,4}, actual);
		}


        [Fact]
        public void Can_serialize_and_deserialize_array()
        {
            var serializer = new XmlMessageSerializer(new DefaultReflection(), new DefaultKernel());
            var stream = new MemoryStream();
            serializer.Serialize(new object[]
            {
                new ClassWithObjectArray
                {
                    Items = new object[] {new OrderLine {Product = "ayende"}}
                }
            }, stream);
            stream.Position = 0;
            var actual = (ClassWithObjectArray) serializer.Deserialize(stream)[0];
            Assert.Equal("ayende", ((OrderLine)actual.Items[0]).Product);
        }

        [Fact]
        public void Trying_to_send_more_than_256_objects_will_fail()
        {
            var serializer = new XmlMessageSerializer(new DefaultReflection(), new DefaultKernel());
            Assert.Throws<UnboundedResultSetException>(() => serializer.Serialize(new object[257], new MemoryStream()));
        }

        [Fact]
        public void Trying_to_send_message_with_list_of_more_than_256_items_will_fail()
        {
            var serializer = new XmlMessageSerializer(new DefaultReflection(), new DefaultKernel());
            var order = new Order
            {
                OrderLines = new OrderLine[257]
            };
            for (int i = 0; i < 257; i++)
            {
              order.OrderLines[i] = new OrderLine();  
            }
            try
            {
                serializer.Serialize(new[] {order}, new MemoryStream());
                Assert.False(true, "should throw");
            }
            catch (SerializationException e)
            {
                Assert.IsType<UnboundedResultSetException>(e.InnerException);  
            }
        }

        [Fact]
        public void Can_deserialize_complex_object_graph()
        {
            var serializer = new XmlMessageSerializer(new DefaultReflection(), new DefaultKernel());
            var stream = new MemoryStream();
            serializer.Serialize(new[] {sample}, stream);
            stream.Position = 0;

            var order = (Order) serializer.Deserialize(stream)[0];

            Assert.Equal(sample.Url, order.Url);
            Assert.Equal(sample.At, order.At);
            Assert.Equal(sample.Count, order.Count);
            Assert.Equal(sample.OrderId, order.OrderId);
            Assert.Equal(sample.TimeToDelivery, order.TimeToDelivery);

            Assert.Equal(2, order.OrderLines.Length);

            Assert.Equal(sample.OrderLines[0].Product, order.OrderLines[0].Product);
            Assert.Equal(sample.OrderLines[1].Product, order.OrderLines[1].Product);
        }

        #region Nested type: ClassWithObjectArray

        public class ClassWithObjectArray
        {
            public object[] Items { get; set; }
        }

        #endregion

        #region Nested type: Order

        public class Order
        {
            public Uri Url { get; set; }
            public int Count { get; set; }
            public Guid OrderId { get; set; }
            public DateTime At { get; set; }
            public TimeSpan TimeToDelivery { get; set; }

            public OrderLine[] OrderLines { get; set; }
        }

        #endregion

        #region Nested type: OrderLine

        public class OrderLine
        {
            public string Product { get; set; }
            public List<int> Fubar { get; set; }
        }

        #endregion
    }
}