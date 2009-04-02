using System.IO;
using Castle.MicroKernel;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Serializers;
using Xunit;

namespace Rhino.ServiceBus.Tests.Bugs
{
    public class Serialization_roundtrip
    {
        public class Foo
        {
            public string Name { get; set; }

            public string UName
            {
                get
                {
                    return Name.ToUpper();
                }
            }
        }

        [Fact]
        public void Can_roundtrip()
        {
            var serializer = new XmlMessageSerializer(new DefaultReflection(),new DefaultKernel());

            var stream = new MemoryStream();
            serializer.Serialize(new object[] {new Foo {Name = "abc"}}, stream);

            stream.Position = 0;

            var foo = (Foo)serializer.Deserialize(stream)[0];
            Assert.Equal("abc", foo.Name);
            Assert.Equal("ABC", foo.UName);
        }
    }
}