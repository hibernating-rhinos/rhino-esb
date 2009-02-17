using System;
using System.IO;
using System.Runtime.Serialization;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class When_Security_Is_Not_Specified_In_Config
    {
        private static IWindsorContainer CreateContainer()
        {
            var container = new WindsorContainer(new XmlInterpreter());
            container.Kernel.AddFacility("rhino.esb", new RhinoServiceBusFacility());
            return container;
        }

        [Fact]
        public void Will_throw_for_wire_encrypted_string()
        {
            var container = CreateContainer();
            var serializer = container.Resolve<IMessageSerializer>();
            var memoryStream = new MemoryStream();
            try
            {
                serializer.Serialize(new[]
                {
                    new When_Security_Is_Specified_In_Config.ClassWithSecretField {ShouldBeEncrypted = "abc"}
                }, memoryStream);
                Assert.True(false, "Expected exception");
            }
            catch (SerializationException e)
            {
                Assert.Equal("Cannot send message containing WireEcryptedString when <security> was not properly set up",
                    e.InnerException.Message);
            }
        }

        [Fact]
        public void Will_not_be_able_to_read_encypted_content()
        {
            var container = CreateContainer();
            var serializer = container.Resolve<IMessageSerializer>();
            var memoryStream = new MemoryStream();
            var writer = new StreamWriter(memoryStream);
            writer.Write(When_Security_Is_Specified_In_Config.encryptedMessage);
            writer.Flush();
            memoryStream.Position = 0;

            var msg = (When_Security_Is_Specified_In_Config.ClassWithSecretField)serializer.Deserialize(memoryStream)[0];

            Assert.True(msg.ShouldBeEncrypted.Value.EndsWith("=="));
        }
    }
}