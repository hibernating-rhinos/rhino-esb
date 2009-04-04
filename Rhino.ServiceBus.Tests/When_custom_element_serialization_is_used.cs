using System;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Xunit;
using Rhino.ServiceBus.Internal;
using System.IO;
using Castle.MicroKernel.Registration;

namespace Rhino.ServiceBus.Tests
{
	public class When_custom_element_serialization_is_used
	{
		private IWindsorContainer container;

		public When_custom_element_serialization_is_used()
		{
			container = new WindsorContainer(new XmlInterpreter());
			container.AddFacility("rhino.esb", new RhinoServiceBusFacility());
			container.Register(
				Component.For<ICustomElementSerializer>()
				.ImplementedBy<DataContractMessageElementSerializer>()
				);
		}

		[Fact]
		public void Can_serialize_custom_element_with_non_custom_element()
		{
			var serializer = container.Resolve<IMessageSerializer>();
			var message1 = new NoDataContract {Value = "test"};
			var message2 = new DataContractMessage {Message = "test2"};
			message2.ContractMessage = message2;
			var memoryStream = new MemoryStream();
			serializer.Serialize(new object[]{message1, message2}, memoryStream);
			memoryStream.Position = 0;
			var messages = serializer.Deserialize(memoryStream);
			var dataContractMessage = messages[1] as DataContractMessage;
			Assert.NotNull(dataContractMessage);
			Assert.Same(dataContractMessage, dataContractMessage.ContractMessage);
		}

		public class NoDataContract
		{
			public string Value { get; set; }
		}

		[DataContract]
		public class DataContractMessage
		{
			[DataMember]
			public string Message { get; set; }

			[DataMember]
			public DataContractMessage ContractMessage { get; set; }
		}

		public class DataContractMessageElementSerializer : ICustomElementSerializer
		{
			public bool CanSerialize(Type type)
			{
				return type == typeof (DataContractMessage);
			}

			public XElement ToElement(object val, Func<Type, XNamespace> getNamespace)
			{
				var serializer = new NetDataContractSerializer();
				using (var ms = new MemoryStream())
				{
					serializer.WriteObject(ms, val);
					ms.Seek(0, SeekOrigin.Begin);
					return XElement.Load(new XmlTextReader(ms));
				}
			}

			public object FromElement(Type type, XElement element)
			{
				var serializer = new NetDataContractSerializer();
				var childElement = element.FirstNode.NextNode;
				return serializer.ReadObject(XmlReader.Create(new StringReader(childElement.ToString())));
			}
		}
	}
}
