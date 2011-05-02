using System;
using System.IO;
using Rhino.ServiceBus.DataStructures;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Sagas.Persisters
{
    public class InMemorySagaPersister<TSaga> : ISagaPersister<TSaga> 
        where TSaga : class, IAccessibleSaga
    {
        private readonly Hashtable<Guid, byte[]> dictionary = new Hashtable<Guid, byte[]>();
        private readonly IServiceLocator serviceLocator;
        private readonly IReflection reflection;
        private readonly IMessageSerializer messageSerializer;

        public InMemorySagaPersister(IServiceLocator serviceLocator, IReflection reflection, IMessageSerializer  messageSerializer)
        {
            this.serviceLocator = serviceLocator;
            this.reflection = reflection;
            this.messageSerializer = messageSerializer;
        }

        public TSaga Get(Guid id)
        {
            byte[] val = null;
            dictionary.Read(reader => reader.TryGetValue(id, out val));
            if(val==null)
                return null;
            using(var ms = new MemoryStream(val))
            {
                var saga = serviceLocator.Resolve<TSaga>();
                saga.Id = id;
                var state = messageSerializer.Deserialize(ms)[0];
                reflection.Set(saga, "State", type => state);
                return saga;
            }
        }

        public void Save(TSaga saga)
        {
            var state = reflection.Get(saga, "State");
            using(var memoryStream = new MemoryStream())
            {
                messageSerializer.Serialize(new[] {state}, memoryStream);
                dictionary.Write(writer => writer.Add(saga.Id, memoryStream.ToArray()));
            }
        }

        public void Complete(TSaga saga)
        {
            dictionary.Write(writer => writer.Remove(saga.Id));
        }
    }
}