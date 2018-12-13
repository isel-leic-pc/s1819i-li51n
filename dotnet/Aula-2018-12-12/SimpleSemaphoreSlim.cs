using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aula_2018_12_12 {

    /// <summary>
    /// A simple semaphore implementaion with wait async support.
    /// The implementation doesn't support cancellation an timeout in order
    /// to make it simples as possible.
    /// The project asynchronizers has variants that support cancellation and callback
    /// </summary>
    class SimpleSemaphoreSlim {

        // the pending queue node requests are simple promises (TaskCompletionSource<bool>)
        internal class Request : TaskCompletionSource<bool> {

        }

        private LinkedList<Request> requests; // the pending acquires list
        private int permits;
        private int maxPermits;

        private object monitor;

        public SimpleSemaphoreSlim(int initial, int maxPermits) {
            this.permits = initial;
            this.maxPermits = maxPermits;
            requests = new LinkedList<Request>();
            monitor = new object();
        }

        /// <summary>
        /// The asynchronous wait (1 unit acquire) operation
        /// </summary>
        /// <returns></returns>
        public Task<bool> WaitAsync() {
            lock(monitor) {
                if (permits > 0) {
                    permits--;
                    return Task.FromResult(true);
                }
                Request r = new Request();
                requests.AddLast(r);
                return r.Task;
            }
        }

        /// <summary>
        /// Synchronous acquire version. 
        /// Is done invoking the asynchronous version and immediately waiting on that one
        /// </summary>
        /// <param name="token"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool Wait() {
            Task<bool> waitTask = WaitAsync();
            try {
                return waitTask.Result;
            }
            catch (AggregateException ae) {
                throw ae.InnerException;
            }

        }
        /// <summary>
        /// The units release operation
        /// </summary>
        /// <param name="units"></param>
        public void Release(int units) {
            lock(monitor) {
                if (permits + units < permits || permits + units > maxPermits)
                    throw new InvalidOperationException();
                permits += units;
               
                while(requests.Count > 0 && permits > 0) {
                    Request r = requests.First.Value;
                    requests.RemoveFirst();
                    permits--;

                    // Now, complete the promise!
                    r.SetResult(true);
                }
            }
        }

    }
}
