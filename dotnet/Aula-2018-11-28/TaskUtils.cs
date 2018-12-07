using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Aula_2018_11_28 {
    class TaskUtils {

        public static Task PCDelay(int millis) {
            Timer t = null;
            TaskCompletionSource<object> tcs = 
                new TaskCompletionSource<object>();
            t = new Timer((o) => {
                t.Dispose();
                tcs.SetResult(null);
            }, null, millis, Timeout.Infinite);

            return tcs.Task;
        }

        public static Task PCDelay(int millis, CancellationToken token) {
            Timer t = null;
            TaskCompletionSource<object> tcs =
                new TaskCompletionSource<object>();
            t = new Timer((o) => {
                t.Dispose();
                tcs.TrySetResult(null);
            }, null, millis, Timeout.Infinite);

            token.Register(() => {
                t.Dispose();
                tcs.TrySetCanceled();
            });
            return tcs.Task;
        }

        public static Task<T[]> PCWhenAll<T>(params Task<T>[] tasks) 
            where T: class{
            TaskCompletionSource<T[]> tcs =
                new TaskCompletionSource<T[]>();
            T[] results = new T[tasks.Length];
            List<AggregateException> exceptions = 
                new List<AggregateException>();

            int remaining = tasks.Length;

            for(int i=0; i < tasks.Length; ++i) {
                tasks[i].ContinueWith((ant, obj) => {
                    int index = (int)obj;
                    if (ant.Status == TaskStatus.RanToCompletion)
                        Volatile.Write(ref results[index], ant.Result);
                    else if (ant.Status == TaskStatus.Faulted)
                        lock(exceptions) exceptions.Add(ant.Exception);
                    if (Interlocked.Decrement(ref remaining) == 0) {
                        if (exceptions.Any())
                            tcs.SetException(exceptions);
                        else
                            tcs.SetResult(results);
                    }

                }, i);
            }
            return tcs.Task;
        }

    }
}
