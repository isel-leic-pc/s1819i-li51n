using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace AsyncUtils {
    public static class TaskCombinators {

        /// <summary>
        /// In this combinator the original tasks are seeing by completion order
        /// in the enumerable returned
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tasks"></param>
        /// <returns></returns>
        public static IEnumerable<Task<T>> OrderByCompletion<T>(
            this IEnumerable<Task<T>> tasks) {

            List<Task<T>> allTasks = tasks.ToList();
            int promiseIndex = -1;
            var completions = new TaskCompletionSource<T>[allTasks.Count];
            var promiseTasks = new Task<T>[allTasks.Count];

            for (int i = 0; i < allTasks.Count; ++i) {
                completions[i] = new TaskCompletionSource<T>();
                promiseTasks[i] = completions[i].Task;
            }

            for (int i = 0; i < allTasks.Count; ++i) {
                allTasks[i].ContinueWith(ant => {
                    int idx = Interlocked.Increment(ref promiseIndex);
                    completions[idx].SetFromTask(ant);
                }, TaskContinuationOptions.ExecuteSynchronously);
            }
            return promiseTasks;
        }
    }
}

