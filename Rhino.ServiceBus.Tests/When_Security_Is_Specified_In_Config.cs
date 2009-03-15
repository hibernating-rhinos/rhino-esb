using System.IO;
using System.Security.Cryptography;
using System.Xml.Linq;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Convertors;
using Rhino.ServiceBus.DataStructures;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Messages;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class When_Security_Is_Specified_In_Config
    {
        private static IWindsorContainer CreateContainer()
        {
            var container = new WindsorContainer(new XmlInterpreter("AnotherBus.config"));
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
            return container;
        }

        [Fact]
        public void Will_register_wire_encrypted_string_convertor_on_container()
        {
            var container = CreateContainer();
            var convertor = container.Resolve<IValueConvertor<WireEcryptedString>>();
            Assert.IsType<WireEcryptedStringConvertor>(convertor);
        }

		[Fact]
		public void Will_register_wire_encrypted_message_convertor_on_container()
		{
			var container = CreateContainer();
			var encryptionBehavior = container.Resolve<IElementSerializationBehavior>();
			Assert.IsType<WireEncryptedMessageConvertor>(encryptionBehavior);
		}


        public class ClassWithSecretField
        {
            public WireEcryptedString ShouldBeEncrypted
            {
                get; set;
            }

        }

		public class SecretMessage : IWireEncryptedMessage
		{
			public int Secret { get; set; }
		}

        public const string encryptedMessage =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<esb:messages xmlns:esb=""http://servicebus.hibernatingrhinos.com/2008/12/20/esb"" xmlns:tests.classwithsecretfield=""Rhino.ServiceBus.Tests.When_Security_Is_Specified_In_Config+ClassWithSecretField, Rhino.ServiceBus.Tests"" xmlns:datastructures.wireecryptedstring=""Rhino.ServiceBus.DataStructures.WireEcryptedString, Rhino.ServiceBus"" xmlns:string=""string"">
  <tests.classwithsecretfield:ClassWithSecretField>
    <datastructures.wireecryptedstring:ShouldBeEncrypted>
      <string:Value iv=""0yL9+t0uyDy9NeP7CU1Wow=="">q9a10IFuRxrzFoZewfdOyg==</string:Value>
    </datastructures.wireecryptedstring:ShouldBeEncrypted>
  </tests.classwithsecretfield:ClassWithSecretField>
</esb:messages>";

        [Fact]
        public void Will_encrypt_fields_of_messages()
        {
            var container = CreateContainer();
            var serializer = container.Resolve<IMessageSerializer>();
            var memoryStream = new MemoryStream();
            serializer.Serialize(new[]
            {
                new ClassWithSecretField
                {
                    ShouldBeEncrypted = new WireEcryptedString{Value = "abc"}
                }
            },memoryStream);

            memoryStream.Position = 0;
            var msg = new StreamReader(memoryStream).ReadToEnd();

            var document = XDocument.Parse(msg);
            var actual = document
                .Element(XName.Get("messages", "http://servicebus.hibernatingrhinos.com/2008/12/20/esb"))
                .Element(XName.Get("ClassWithSecretField","Rhino.ServiceBus.Tests.When_Security_Is_Specified_In_Config+ClassWithSecretField, Rhino.ServiceBus.Tests"))
                .Element(XName.Get("ShouldBeEncrypted","Rhino.ServiceBus.DataStructures.WireEcryptedString, Rhino.ServiceBus"))
                .Element(XName.Get("Value","string"))
                .Value;

            Assert.NotEqual("abc", actual);
        }

        [Fact]
        public void Will_decrypt_fields_of_messages()
        {
            var container = CreateContainer();
            var serializer = container.Resolve<IMessageSerializer>();
            var memoryStream = new MemoryStream();
            serializer.Serialize(new[]
            {
                new ClassWithSecretField
                {
                    ShouldBeEncrypted = new WireEcryptedString{Value = "abc"}
                }
            }, memoryStream);

            memoryStream.Position = 0;
            
            var msg = (ClassWithSecretField)serializer.Deserialize(memoryStream)[0];

            Assert.Equal("abc", msg.ShouldBeEncrypted.Value);
        }

		[Fact]
		public void Will_encrypt_entire_message_for_wire_encrypted_message()
		{
			var container = CreateContainer();
			var serializer = container.Resolve<IMessageSerializer>();
			var memoryStream = new MemoryStream();
			serializer.Serialize(new[]
            {
                new SecretMessage
                {
                    Secret = 1234,
                }
            }, memoryStream);

			memoryStream.Position = 0;
			var msg = new StreamReader(memoryStream).ReadToEnd();

			var document = XDocument.Parse(msg);
			var secretMessage = document
				.Element(XName.Get("messages", "http://servicebus.hibernatingrhinos.com/2008/12/20/esb"))
				.Element(XName.Get("SecretMessage", "Rhino.ServiceBus.Tests.When_Security_Is_Specified_In_Config+SecretMessage, Rhino.ServiceBus.Tests"));

			var secret = secretMessage.Element(XName.Get("Secret", "int"));
			Assert.Null(secret);

			var valueElement = secretMessage.Element(XName.Get("Value", "string"));
			Assert.NotNull(valueElement);

			var iv = valueElement.Attribute("iv");
			Assert.NotNull(iv);
		}

		[Fact]
		public void Will_decrypt_message_for_wire_encrypted_message()
		{
			var container = CreateContainer();
			var serializer = container.Resolve<IMessageSerializer>();
			var memoryStream = new MemoryStream();
			serializer.Serialize(new[]
            {
                new SecretMessage 
                {
					Secret = 1234,
                }
            }, memoryStream);

			memoryStream.Position = 0;

			var msg = (SecretMessage)serializer.Deserialize(memoryStream)[0];

			Assert.Equal(1234, msg.Secret);
		}

        [Fact]
        public void When_key_is_different_deserializing_key_will_fail()
        {
            var container = CreateContainer();
            var serializer = container.Resolve<IMessageSerializer>();
            var convertor = (WireEcryptedStringConvertor)container.Resolve<IValueConvertor<WireEcryptedString>>();

            var managed = new RijndaelManaged();
            managed.GenerateKey();

            convertor.EncryptionService.Key = managed.Key;

            var memoryStream = new MemoryStream();
            var writer = new StreamWriter(memoryStream);
            writer.Write(encryptedMessage);
            writer.Flush();
            memoryStream.Position = 0;

            Assert.Throws<CryptographicException>(
                () => serializer.Deserialize(memoryStream)
                );
        }
    }
}
