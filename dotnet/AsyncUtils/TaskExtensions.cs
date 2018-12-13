using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncUtils {
    public static class TaskExtensions {
        
        /// <summary>
        /// An auxiliary extension method to force a TaskCompletionSource to complete with
        /// the completion status of the given task
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tcs"></param>
        /// <param name="t"></param>
        public static void SetFromTask<T>(this TaskCompletionSource<T> tcs, Task<T> t) {
            switch (t.Status) {
                case TaskStatus.Canceled:
                    tcs.TrySetCanceled();
                    break;
                case TaskStatus.Faulted:
                    tcs.TrySetException(t.Exception.InnerException);
                    break;
                case TaskStatus.RanToCompletion:
                    tcs.TrySetResult(t.Result);
                    break;
            }
        }

        /// <summary>
        /// A variant of the previous method but now with a Task withous result
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tcs"></param>
        /// <param name="t"></param>
        public static void SetFromTask(this TaskCompletionSource<object> tcs, Task t) {
            switch (t.Status) {
                case TaskStatus.Canceled:
                    tcs.TrySetCanceled();
                    break;
                case TaskStatus.Faulted:
                    tcs.TrySetException(t.Exception.InnerException);
                    break;
                case TaskStatus.RanToCompletion:
                    tcs.TrySetResult(null);
                    break;
            }
        }

        /// <summary>
        /// Aa extension method suggesting a possible (but far from real implementation)
        /// of the "Unwrap" extension method.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <returns></returns>
        public static Task<T> Unwrap2<T>(this Task<Task<T>> task) {
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
            task.ContinueWith((ant) => {
                switch (ant.Status) {
                    case TaskStatus.Canceled:
                        tcs.TrySetCanceled();
                        break;
                    case TaskStatus.Faulted:
                        tcs.TrySetException(ant.Exception.InnerExceptions);
                        break;
                    case TaskStatus.RanToCompletion:
                        Task<T> tr = ant.Result;
                        tr.ContinueWith((ant2) => {
                            tcs.SetFromTask(ant2);
                        });
                        break;
                }

            });
            return tcs.Task;
        }

        /// <summary>
        /// An extension method provide continuations for task results 
        /// similar to continuations of the CompletableFuture in Java.
        /// Using this one the Unwrap methods are not necessary
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="t"></param>
        /// <param name="cont"></param>
        /// <returns></returns>
        public static Task<U> AndThen<T, U>(this Task<T> t, Func<T, Task<U>> cont) {
            TaskCompletionSource<U> tcs = new TaskCompletionSource<U>();

            t.ContinueWith((ant) => {
                switch (ant.Status) {
                    case TaskStatus.Canceled:
                        tcs.TrySetCanceled();
                        break;
                    case TaskStatus.Faulted:
                        tcs.SetException(ant.Exception);
                        break;
                    case TaskStatus.RanToCompletion:
                        cont(ant.Result).ContinueWith((ant2) => {
                            tcs.SetFromTask(ant2);
                        }, TaskContinuationOptions.ExecuteSynchronously);
                        break;
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
            return tcs.Task;
        }

        /// <summary>
        /// A variant of the previous method now receiving a Task without result
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="t"></param>
        /// <param name="cont"></param>
        /// <returns></returns>
        public static Task<U> AndThen<U>(this Task t, Func<Task<U>> cont) {
            TaskCompletionSource<U> tcs = new TaskCompletionSource<U>();

            t.ContinueWith((ant) => {
                switch (ant.Status) {
                    case TaskStatus.Canceled:
                        tcs.TrySetCanceled();
                        break;
                    case TaskStatus.Faulted:
                        tcs.SetException(ant.Exception);
                        break;
                    case TaskStatus.RanToCompletion:
                        cont().ContinueWith((ant2) => {
                            tcs.SetFromTask(ant2);
                        }, TaskContinuationOptions.ExecuteSynchronously);
                        break;
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
            return tcs.Task;

        }

        /// <summary>
        /// A variant of the previous method but now returning a Task without result
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="t"></param>
        /// <param name="cont"></param>
        /// <returns></returns>
        public static Task AndThen<T>(this Task<T> t, Func<T, Task> cont) {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            t.ContinueWith((ant) => {
                switch (ant.Status) {
                    case TaskStatus.Canceled:
                        tcs.TrySetCanceled();
                        break;
                    case TaskStatus.Faulted:
                        tcs.SetException(ant.Exception);
                        break;
                    case TaskStatus.RanToCompletion:
                        cont(ant.Result).ContinueWith((ant2) => {
                            tcs.SetFromTask(ant2);
                        }, TaskContinuationOptions.ExecuteSynchronously);
                        break;
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
            return tcs.Task;
        }

        public static Task<T> WithTimeout<T>(this Task<T> task, int timeout, Action onTimeout) {
            var promise = new TaskCompletionSource<T>();
            Task delay = Task.Delay(timeout);
            Task.WhenAny(task, Task.Delay(timeout)).
                ContinueWith(ant => {
                    Task t = ant.Result;
                    if (t == task) {
                        promise.SetResult(task.Result);
                    }
                    else {
                        onTimeout();
                        promise.SetException(new TimeoutException());
                    }
                });
            return promise.Task;
        }

        public static Task<T> WithTimeout2<T>(this Task<T> task, int timeout) {
            var tcs = new TaskCompletionSource<T>();

            //
            // Create a timer that will be a completion source for the 
            // proxy task associated with the task completion source.
            //

            var timer = new Timer(_ => tcs.TrySetException(new TimeoutException()));
            timer.Change(timeout, Timeout.Infinite);

            //
            // The task argument will also be a completion source, racing with
            // the timer to set the final state of the proxy task.
            //

            task.ContinueWith(t => {
                timer.Dispose();
                tcs.TrySetFromTask(t);
            });

            //
            // Return the proxy task.
            //

            return tcs.Task;
        }
    }
}
