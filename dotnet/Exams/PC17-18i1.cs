using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Testes {
    class PC17_18i1 {
        private class BoundedOrderedQueue<T> {
            public BoundedOrderedQueue(int capacity) {

            }
            public void Add(T item) {
            }
            public void Merge(BoundedOrderedQueue<T> ol) {  
            }
            public T[] ToArray() {
                return null;

            }
        }

        /// <summary>
        /// Synchronous version
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="topSize"></param>
        /// <returns></returns>
        public T[] GetTopN<T>(IEnumerable<T> items, int topSize) where T : IComparable<T> {
            BoundedOrderedQueue<T> queue = new BoundedOrderedQueue<T>(topSize);
            foreach (T i in items)
                queue.Add(i);
            return queue.ToArray();

        }


        public T[] GetTopNPar<T>(IEnumerable<T> items, int topSize) where T : IComparable<T> {
            BoundedOrderedQueue<T> queue = new BoundedOrderedQueue<T>(topSize);
            object monitor = new object();

            Parallel.ForEach(
                items,
                () => new BoundedOrderedQueue<T>(topSize), // initial state
                (it, state, partial) => {             // local body
                    partial.Add(it);
                    return partial;
                },
                (p) => {                              // final aggregation
                    lock (monitor)
                        queue.Merge(p);
                });

            return queue.ToArray();

        }
    }
}

