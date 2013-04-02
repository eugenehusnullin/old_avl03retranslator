using System;
using System.Collections.Generic;
using System.Threading;

namespace TcpServer.Core.Mintrans
{
    public class ObjectPool<T>
        where T : class
    {
        private readonly int maxSize;
        private Func<T> constructor;
        private int currentSize;
        private Queue<T> pool;
        private AutoResetEvent poolReleasedEvent;

        public ObjectPool(int maxSize, Func<T> constructor)
        {
            this.maxSize = maxSize;
            this.constructor = constructor;
            this.currentSize = 0;
            this.pool = new Queue<T>();
            this.poolReleasedEvent = new AutoResetEvent(false);
        }

        public T GetFromPool()
        {
            T item = null;
            do
            {
                lock (this)
                {
                    if (this.pool.Count == 0)
                    {
                        if (this.currentSize < this.maxSize)
                        {
                            item = this.constructor();
                            this.currentSize++;
                        }
                    }
                    else
                    {
                        item = this.pool.Dequeue();
                    }
                }

                if (null == item)
                {
                    this.poolReleasedEvent.WaitOne();
                }
            }
            while (null == item);
            return item;
        }

        public void ReturnToPool(T item)
        {
            lock (this)
            {
                this.pool.Enqueue(item);
                this.poolReleasedEvent.Set();
            }
        }

        public void DestroyOne()
        {
            lock (this)
            {
                this.currentSize--;
                this.poolReleasedEvent.Set();
            }
        }
    }
}