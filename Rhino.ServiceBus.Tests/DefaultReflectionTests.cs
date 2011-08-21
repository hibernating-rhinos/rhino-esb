using System;
using System.Collections.Generic;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Sagas;
using Xunit;

namespace Rhino.ServiceBus.Tests
{
    public class DefaultReflectionTests
    {
        private readonly IReflection reflection = new DefaultReflection();

        [Fact]
        public void Can_roundtrip_uri()
        {
            object msg = new Uri("http://ayende.com");
            var typeName = reflection.GetNamespaceForXml(msg.GetType());
            var type = reflection.GetTypeFromXmlNamespace(typeName);
            Assert.NotNull(type);
        }

        [Fact]
        public void Message_type_harvesting_returns_consumers()
        {
            var types = reflection.GetMessagesConsumed(typeof(SomeMsgConsumer), t => false);
            Assert.Equal(1, types.Length);
            Assert.Equal(types[0], typeof(SomeMsg));
        }

        [Fact]
        public void Message_type_harvesting_ignores_open_generic_consumers()
        {
            var types = reflection.GetMessagesConsumed(typeof(GenericConsumer<>), t => false);
            Assert.Empty(types);
        }

        [Fact]
        public void Will_throw_inner_exception_for_add()
        {
            Assert.Throws<InvalidCastException>(() => reflection.InvokeAdd(new ThrowingList(), 1));
        }

        [Fact]
        public void Will_throw_inner_exception_for_consume()
        {
            Assert.Throws<NotImplementedException>(() => reflection.InvokeConsume(new ThrowingConsumer(), "1"));
        }

        [Fact]
        public void Will_throw_inner_exception_for_complete()
        {
            Assert.Throws<NotImplementedException>(() => reflection.InvokeSagaPersisterComplete(new ThrowingPersister(), new ThrowingList()));
        }

        [Fact]
        public void Will_throw_inner_exception_for_get()
        {
            Assert.Throws<NotImplementedException>(() => reflection.InvokeSagaPersisterGet(new ThrowingPersister(), Guid.NewGuid()));
        }

        [Fact]
        public void Will_throw_inner_exception_for_save()
        {
            Assert.Throws<NotImplementedException>(() => reflection.InvokeSagaPersisterSave(new ThrowingPersister(), new ThrowingList()));
        }
        [Fact]
        public void Gets_assembly_name_without_version_for_generic_lists()
        {
            var list = new List<SomeMsg>();
            var output = reflection.GetNamespaceForXml(list.GetType());

            Assert.DoesNotContain("Rhino.ServiceBus.Tests,Version=", output.Replace(" ", ""));
        }
        [Fact]
        public void Gets_assembly_name_without_version_for_generic_types_in_local_assemblies()
        {
            var output = reflection.GetNamespaceForXml(typeof(GenericConsumer<SomeMsg>));

            Assert.DoesNotContain("Rhino.ServiceBus.Tests,Version=", output.Replace(" ", ""));
            Assert.NotNull(Type.GetType(output));  // still fail!!!
        }

        [Fact]
        public void Gets_assembly_name_with_more_than_one_type_parameter()
        {
            string name = reflection.GetNamespaceForXml(typeof(Dictionary<object, object>));
            Assert.Equal(
                typeof(Dictionary<object, object>).AssemblyQualifiedName,
                name);
            var type = Type.GetType(name);
        }

        [Fact]
        public void Gets_assembly_name_without_version_for_generic_types_with_more_than_one_type_parameter_in_local_assemblies()
        {
            string name = reflection.GetNamespaceForXml(typeof(TestDictionary<string, string>));
            var type = Type.GetType(name);
            Assert.NotNull(type);   // fail if not apply my fix!!!
        }



        [Fact]
        public void Gets_all_consumers_for_message_type_with_interface_and_base_class()
        {
            var consumers = reflection.GetGenericTypesOfWithBaseTypes(typeof(ConsumerOf<>), new SomeMsg2());
            Assert.Equal(4, consumers.Count);
            Assert.Contains(typeof(ConsumerOf<SomeMsg2>), consumers);
            Assert.Contains(typeof(ConsumerOf<SomeMsg>), consumers);
            Assert.Contains(typeof(ConsumerOf<IAmASpecialMessage>), consumers);
            Assert.Contains(typeof(ConsumerOf<object>), consumers);
        }

        [Fact]
        public void Can_CreateInstance_when_type_has_private_constructor()
        {
            object objectFromClassWithPrivateConstructor = reflection.CreateInstance(typeof(ClassWithPrivateConstructor), new object[] { });
            Assert.NotNull(objectFromClassWithPrivateConstructor);
        }

        public class SomeMsg { }
        public class SomeMsgConsumer : GenericConsumer<SomeMsg> { }
        public class GenericConsumer<T> : ConsumerOf<T>
        {
            public void Consume(T message)
            {
            }
        }

        public interface IAmASpecialMessage { }

        public class SomeMsg2 : SomeMsg, IAmASpecialMessage { }

        public class SomeMsg2Consumer : GenericConsumer<SomeMsg2> { }
        public class IAmASpecialMessageConsumer : GenericConsumer<IAmASpecialMessage> { }

        public class ThrowingConsumer : ConsumerOf<string>
        {
            public void Consume(string message)
            {
                throw new System.NotImplementedException();
            }
        }

        public class ThrowingList : ISaga<int>
        {
            public void Add(object i)
            {
                throw new InvalidCastException();
            }

            public Guid Id
            {
                get { throw new System.NotImplementedException(); }
                set { throw new System.NotImplementedException(); }
            }

            public bool IsCompleted
            {
                get { throw new System.NotImplementedException(); }
                set { throw new System.NotImplementedException(); }
            }

            public int State
            {
                get { throw new System.NotImplementedException(); }
                set { throw new System.NotImplementedException(); }
            }
        }

        public class ThrowingPersister : ISagaPersister<DefaultReflectionTests.ThrowingList>
        {
            public ThrowingList Get(Guid id)
            {
                throw new System.NotImplementedException();
            }

            public void Save(ThrowingList saga)
            {
                throw new System.NotImplementedException();
            }

            public void Complete(ThrowingList saga)
            {
                throw new System.NotImplementedException();
            }
        }

        public class ClassWithPrivateConstructor
        {
            private ClassWithPrivateConstructor() { }
        }

    }
    public class TestDictionary<T, TK> : Dictionary<T, TK>
    {

    }


}
