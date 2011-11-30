using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Castle.MicroKernel;
using Castle.Windsor;
using Rhino.ServiceBus.Castle;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Serializers;
using Xunit;
using Xunit.Extensions;

namespace Rhino.ServiceBus.Tests.Bugs
{
    [CLSCompliant(false)]
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

		public class ItemWithInitDictionary
		{
			public string Name { get; set; }
			public Dictionary<string, string> Arguments { get; set; }

			public ItemWithInitDictionary()
			{
				Arguments = new Dictionary<string, string>();
			}
		}

		public class ItemWithoutInitDictionary
		{
			public string Name { get; set; }
			public Dictionary<string, string> Arguments { get; set; }
		}

		[Fact]
		public void Can_use_dictionaries_initialized()
		{
			var serializer = new XmlMessageSerializer(new DefaultReflection(),
													new CastleServiceLocator(new WindsorContainer()));

			var stream = new MemoryStream();
			serializer.Serialize(new object[] { new ItemWithInitDictionary { Name = "abc", Arguments = new Dictionary<string, string>
			{
				{"abc","cdef"}
			}} }, stream);

			stream.Position = 0;

			var foo = (ItemWithInitDictionary)serializer.Deserialize(stream)[0];
			Assert.Equal("abc", foo.Name);
			Assert.Equal("cdef", foo.Arguments["abc"]);
		}

		[Fact]
		public void Can_use_dictionaries_uninitialized()
		{
			var serializer = new XmlMessageSerializer(new DefaultReflection(),
													new CastleServiceLocator(new WindsorContainer()));

			var stream = new MemoryStream();
			serializer.Serialize(new object[] { new ItemWithoutInitDictionary { Name = "abc", Arguments = new Dictionary<string, string>
			{
				{"abc","cdef"}
			}} }, stream);

			stream.Position = 0;

			stream.Position = 0;


			var foo = (ItemWithoutInitDictionary)serializer.Deserialize(stream)[0];
			Assert.Equal("abc", foo.Name);
			Assert.Equal("cdef", foo.Arguments["abc"]);
		}

        [Fact]
        public void Can_roundtrip()
        {
            var serializer = new XmlMessageSerializer(new DefaultReflection(),
                                                      new CastleServiceLocator(new WindsorContainer()));

            var stream = new MemoryStream();
            serializer.Serialize(new object[] { new Foo { Name = "abc" } }, stream);

            stream.Position = 0;

            var foo = (Foo)serializer.Deserialize(stream)[0];
            Assert.Equal("abc", foo.Name);
            Assert.Equal("ABC", foo.UName);
        }

        [Theory]
        [InlineData(DateTimeKind.Local)]
        [InlineData(DateTimeKind.Unspecified)]
        [InlineData(DateTimeKind.Utc)]
        public void Roundtrip_with_datetime_should_preserved_DateTimeKind(DateTimeKind kind)
        {
            var serializer = new XmlMessageSerializer(new DefaultReflection(),
                                           new CastleServiceLocator(new WindsorContainer()));
            var stream = new MemoryStream();
            var date = new DateTime(DateTime.Now.Ticks, kind);
            serializer.Serialize(new object[] { new Bar { Date = date } }, stream);

            stream.Position = 0;
            
            var bar = (Bar)serializer.Deserialize(stream)[0];
            Assert.Equal(date, bar.Date);  
            Assert.Equal(kind, bar.Date.Kind);
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
                var serializer = new XmlMessageSerializer(new DefaultReflection(),
                                                      new CastleServiceLocator(new WindsorContainer()));
            
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