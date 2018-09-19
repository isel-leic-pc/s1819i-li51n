using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Aula_2018_09_18
{
    class BoundedQueueMRE<T> : IBoundedQueue<T>
    {
        private readonly LinkedList<T> elems;
        private int size;
        private readonly int capacity;
        private Mutex mutex;
        private ManualResetEvent availableSpace, availableElems;

        private bool IsEmpty() { return size == 0; }

        private bool IsFull() { return size == capacity; }

        public BoundedQueueMRE(int capacity)
        {
            size = 0;
            this.capacity = capacity;
            elems = new LinkedList<T>();
            availableElems = new ManualResetEvent(false);
            availableSpace = new ManualResetEvent(true);
            mutex = new Mutex();
        }

        public T Get()
        {
            mutex.WaitOne();
            while (IsEmpty())
            {
                mutex.ReleaseMutex();
                // Exacerbate vulnerability window size
                Thread.Sleep(10);
                availableElems.WaitOne();
                mutex.WaitOne();
            }
            T e = elems.First();
            elems.RemoveFirst();
            if (--size == 0)
                availableElems.Reset();
            if (size == capacity - 1)
                availableSpace.Set();
            mutex.ReleaseMutex();

            return e;
        }

        public void Put(T e)
        {
            mutex.WaitOne();
            while (IsFull())
            {
                mutex.ReleaseMutex();

                availableSpace.WaitOne();
                mutex.WaitOne();
            }
            elems.AddLast(e);
            if (++size == capacity)
                availableSpace.Reset();
            if (size == 1)
                availableElems.Set();
            mutex.ReleaseMutex();

        }

        public int Capacity => capacity;
    }
}
