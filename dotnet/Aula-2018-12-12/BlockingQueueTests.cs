using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aula_2018_12_12 {
    class BlockingQueueTests {

        private bool AllValuesSorted(int[] vals, int first, int last) {
            if (vals.Length != (last - first + 1)) return false;
            for(int i=0; i < vals.Length; ++i) {
                if (vals[i] != first + i) return false;
            }
            return true;
        }

        [Test]
        public void SimpleProducerConsumerSynchTest() {
            const int CAPACITY = 100;
            const int NITEMS = CAPACITY * 2;
            BlockingQueue<object> numbers = new BlockingQueue<object>(CAPACITY);
            var result = new int[NITEMS];

            var consumer = Task.Run(() => {
                for (int i = 1; i <= NITEMS; ++i) {
                    object r;
                    if ((r = numbers.Take()) != null)
                        result[i-1] = (int) r;
                }
            });

            var producer = Task.Run(() => {
                for (int i = 1; i <= NITEMS; ++i) {
                    numbers.Put(i);
                }
            });

            Task.WaitAll(producer, consumer);

            Assert.IsTrue(AllValuesSorted(result, 1, NITEMS));
        }

        [Test]
        public void SimpleProducerConsumerAsynchTest() {
            const int CAPACITY = 10;
            const int NITEMS = CAPACITY * 2;
            BlockingQueue<object> numbers = new BlockingQueue<object>(CAPACITY);
            var result = new int[NITEMS];

            // the consumer asynchronous lambda
            Func<Task<bool>> consumer = async () => {
                Console.WriteLine("Consumer in main thread {0}",
                    Thread.CurrentThread.ManagedThreadId);
                for (int i = 1; i <= NITEMS; ++i) {
                    object r;
                    if ((r = await numbers.TakeAsync()) != null) {
                        Console.WriteLine("Consumer in continuation thread {0}",
                            Thread.CurrentThread.ManagedThreadId);
                        result[i - 1] = (int)r;
                    }                      
                }
                return true;
            };

            // the producer asynchronous lambda
            Func<Task<bool>> producer = async () => {
                Console.WriteLine("Producer in main thread {0}",
                    Thread.CurrentThread.ManagedThreadId);
                for (int i = 1; i <= NITEMS; ++i) {
                    await numbers.PutAsync(i);
                    Console.WriteLine("Producer in continuation thread {0}",
                           Thread.CurrentThread.ManagedThreadId);
                    Thread.Sleep(1000);
                }
                return true;
            };

            var t1 = producer();
            var t2 = consumer();

            Task.WaitAll(t1, t2);

            Assert.IsTrue(AllValuesSorted(result, 1, NITEMS));
        }
    }
}
