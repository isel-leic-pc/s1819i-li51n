using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
namespace TPL1
{
    public class TaskUtils
    {
        public static IEnumerable<Task<T>> OrderByCompletion<T>(Task<T>[] tasks) {
            TaskCompletionSource<T>[] completions  = new TaskCompletionSource<T>[tasks.Length];
            Task<T>[] completed = new Task<T>[tasks.Length];
            int order = -1;
            for (int i = 0; i < tasks.Length; ++i) {
                completions[i] = new TaskCompletionSource<T>();
                completed[i] = completions[i].Task;
            }
            foreach( Task<T> t in tasks) {
                t.ContinueWith(ant => {
                    if (t.IsCompleted) {
                        int p = Interlocked.Increment(ref order);
                        completions[p].SetResult(t.Result);
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);
            }
            return completed;
        }
    }
}
