using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Asynchronizers {
    /**
 * A blocking queue with synchronous and asynchronous TAP interface
 */
    internal class BlockingQueueAsync<T> where T : class {
        private readonly int capacity;
        private readonly T[] room;
        private readonly SemaphoreSlim1 freeSlots, filledSlots;
        private int putIdx, takeIdx;

        // construct the blocking queue
        public BlockingQueueAsync(int capacity) {
            this.capacity = capacity;
            this.room = new T[capacity];
            this.putIdx = this.takeIdx = 0;
            this.freeSlots = new SemaphoreSlim1(capacity, capacity);
            this.filledSlots = new SemaphoreSlim1(0, capacity);
        }

        // Put an item in the queue asynchronously enabling timeout and cancellation
        public async Task<bool> PutAsync(T item, int timeout = Timeout.Infinite,
                                         CancellationToken cToken = default(CancellationToken)) {
            if (!await freeSlots.AcquireAsync(timeout: timeout, token: cToken))
                return false;       // timed out
            lock (room)
                room[putIdx++ % capacity] = item;
            filledSlots.Release();
            return true;
        }

        // Put an item in the queue synchronously enabling timeout and cancellation
        public bool Put(T item, int timeout = Timeout.Infinite,
                        CancellationToken cToken = default(CancellationToken)) {
            if (freeSlots.Acquire(timeout: timeout, token: cToken)) {
                lock (room)
                    room[putIdx++ % capacity] = item;
                filledSlots.Release();
                return true;
            }
            else
                return false;
        }

        // Take an item from the queue asynchronously enabling timeout and cancellation
        public async Task<T> TakeAsync(int timeout, CancellationToken cToken) {
            if (await filledSlots.AcquireAsync(timeout: timeout, token: cToken)) {
                T item;
                lock (room)
                    item = room[takeIdx++ % capacity];
                freeSlots.Release();
                return item;
            }
            else
                return null;        // timed out
        }

        // Take an item from the queue synchronously enabling timeout and cancellation
        public T Take(int timeout = Timeout.Infinite,
                      CancellationToken cToken = default(CancellationToken)) {
            if (filledSlots.Acquire(timeout: timeout, token: cToken)) {
                T item;
                lock (room)
                    item = room[takeIdx++ % capacity];
                freeSlots.Release();
                return item;
            }
            else
                return null;
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
