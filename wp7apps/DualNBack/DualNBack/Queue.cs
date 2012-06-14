using System.Collections.Generic;

namespace DualNBack
{
    public class Queue<T>
    {
        private readonly int size;
        private List<T> list = new List<T>();

        public Queue( int size )
        {
            this.size = size;
        }

        public void Add( T value )
        {
            if(list.Count >= size) list.RemoveAt(0);
            list.Add(value);
        }

        public T First() {
            if(this.IsFull()) return list[0];
            else throw new QueueNotFullException();
        }

        public T Last() {
            return this.list[this.size - 1];
        }

        public bool IsFull() {
            return this.list.Count == this.size;
        }
    }
}
