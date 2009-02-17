using System.Collections.Generic;

namespace Rhino.ServiceBus.DataStructures
{
    public class Queue<T>
        where T : class
    {
        private readonly LinkedList<T> list = new LinkedList<T>();

        public int TotalCount
        {
            get
            {
                lock (list)
                    return list.Count;
            }
        }

        public void Enqueue(T value)
        {
            lock (list)
                list.AddLast(value);
        }

        public T Dequeue()
        {
            lock(list)
            {
                if (list.Count == 0)
                    return null;
                var value = list.First.Value;
                list.RemoveFirst();
                return value;
            }
        }

        public void Clear()
        {
            lock(list)
                list.Clear();
        }
    }
}