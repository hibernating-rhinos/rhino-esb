using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Castle.MicroKernel;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Serializers;
using Xunit;
using Xunit.Extensions;

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

        public class Bar
        {
            public DateTime Date { get; set; }
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

        [Theory]
        [InlineData("it-IT")]
        [InlineData("mi-NZ")]
        [InlineData("es-US")]
        public void Can_roundtrip_with_datetime_on_non_english_culture(string cultureName)
        {
            var oldCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo(cultureName);

            try
            {
                var serializer = new XmlMessageSerializer(new DefaultReflection(), new DefaultKernel());
            
                var stream = new MemoryStream();
                var date = DateTime.Now;
                serializer.Serialize(new object[] { new Bar { Date = date } }, stream);

                stream.Position = 0;

                var bar = (Bar)serializer.Deserialize(stream)[0];
                Assert.Equal(date, bar.Date);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = oldCulture;
            }
        }
    }
}