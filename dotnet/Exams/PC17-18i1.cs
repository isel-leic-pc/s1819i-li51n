using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Testes {
    class PC17_18i1 {

        // MapReduce

        public R Map<T, R>(T t) { return default(R); }
        public R Reduce<R>(R r1, R r2) { return default(R); }

        /// <summary>
        /// Versão sequencial(síncrona)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="elems"></param>
        /// <param name="initial"></param>
        /// <returns></returns>
        public R MapReduce<T, R>(T[] elems, R initial) {
            for (int i = 0; i < elems.Length; ++i)
                initial = Reduce(Map<T, R>(elems[i]), initial);
            return initial;
        }

        /// Variantes assíncronas de Map e Reduce
        /// 
        public Task<R> MapAsync<T, R>(T t) { return null; }
        public Task<R> ReduceAsync<R>(R r1, R r2) { return null; }

        /// <summary>
        /// Versão assíncrona sem preocupação em tratar eventuais execepções
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="elems"></param>
        /// <param name="initial"></param>
        /// <returns></returns>
        public async Task<R> MapReduceAsync<T, R>(T[] elems, R initial) {
            Task<R>[] tres = new Task<R>[elems.Length];

            for (int i = 0; i < elems.Length; ++i)
                tres[i] = MapAsync<T, R>(elems[i]);

            // reduce by order
            for (int i = 0; i < elems.Length; ++i)
                initial = await ReduceAsync(await tres[i], initial);
            
            return initial;
        }


        // GetTopN
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

