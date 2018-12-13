using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aula_2018_12_12 {

    /// <summary>
    /// A blocking queue with Put and Take async variants
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BlockingQueue<T> where T: class {
        private readonly int capacity;
        private readonly T[] room;
        private readonly SemaphoreSlim  freeSlots, filledSlots;
        private int putIdx, takeIdx;

        // construct the blocking queue
        public BlockingQueue(int capacity) {
            this.capacity = capacity;
            room = new T[capacity];
            putIdx = this.takeIdx = 0;
            freeSlots = new SemaphoreSlim(capacity, capacity);
            filledSlots = new SemaphoreSlim(0, capacity);
        }

        // Put an item in the queue synchronously enabling timeout  
        public bool Put(T item, int timeout = Timeout.Infinite) {
            if (!freeSlots.Wait(timeout)) return false;

            lock (room)
                room[putIdx++ % capacity] = item;
            filledSlots.Release();
            return true;   
        }

        // Put an item in the queue asynchronously enabling timeout  
        public async Task<bool> PutAsync(T item, int timeout = Timeout.Infinite) {
            // here we are supported by the WaitAsync of SemaphoreSlim implementation!
            if (! await freeSlots.WaitAsync(timeout)) return false;

            lock (room)
                room[putIdx++ % capacity] = item;
            filledSlots.Release();
            return true;
        }



        // Take an item from the queue synchronously enabling timeout 
        public T Take( int timeout = Timeout.Infinite) {
            if (!filledSlots.Wait(timeout)) return null;
            T item;
            lock (room)
                item = room[takeIdx++ % capacity];
            freeSlots.Release();
               
            return item;   
        }

        // Take an item from the queue synchronously enabling timeout 
        public async Task<T> TakeAsync(int timeout = Timeout.Infinite) {
            // here we are supported by the WaitAsync of SemaphoreSlim implementation!
            if (! await filledSlots.WaitAsync(timeout)) return null;
            T item;
            lock (room)
                item = room[takeIdx++ % capacity];
            freeSlots.Release();

            return item;
        }


        // Returns the number of filled positions in the queue
        public int Count {
            get {
                lock (room)
                    return putIdx - takeIdx;
            }
        }
    }

}
