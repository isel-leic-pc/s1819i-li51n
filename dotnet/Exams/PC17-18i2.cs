using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Testes {

    public class Question4_1718 {
        public static R Map<T, R>(T t) { return default(R); }
        public static R Join<R>(R r, R r2) { return default(R); }

        public static Task<R> MapAsync<T, R>(T t) { return Task.Run(() => { return default(R); }); }
        public static Task<R> JoinAsync<R>(R r, R r2) { return Task.Run(() => { return default(R); }); }

        public static R[] MapJoin<T,R>(T[] items) {
            var res = new R[items.Length / 2];

            for (int i = 0; i < res.Length; i++)
                res[i] = Join(Map<T,R>(items[2 * i]), Map<T,R>(items[2 * i + 1]));
            return res;
        }

        public static async Task<R> Join2Async<T,R>(T i1, T i2) {
            R[] resp = await Task.WhenAll<R>(MapAsync<T,R>(i1), MapAsync<T,R>(i2));
            return await JoinAsync(resp[0], resp[1]);
        }

        public static async Task<R[]> MapJoinAsync<T, R>(T[] items) {
            var tasks = new Task<R>[items.Length / 2];
            for (int i = 0; i < tasks.Length; i++)
                tasks[i] = Join2Async<T,R>(items[2 * i], items[2 * i + 1]);
            return await Task.WhenAll(tasks);
        }


        public static bool AtLeastOccursParallel1<T>(IEnumerable<T> items, Predicate<T> selector, int occurrences, CancellationToken ctoken) {
            object monitor = new object();
            int result = 0;
            var options = new ParallelOptions { CancellationToken = ctoken};

            Parallel.ForEach(
                items,
                options,
                () => 0,
                (item, state, parcial) => {
                    options.CancellationToken.ThrowIfCancellationRequested();
                    if (selector(item)) {
                        parcial++;
                        if (parcial >= occurrences) {
                            state.Stop();
                        }
                    }

                    return parcial;
                },
                (parcial) => {
                    if (Volatile.Read(ref result) < occurrences)
                        Interlocked.Add(ref result, parcial);
                    
                });
            return result >= occurrences;

        }

    }

   


}
