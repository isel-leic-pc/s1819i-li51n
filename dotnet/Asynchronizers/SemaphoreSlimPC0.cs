using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace Asynchronizers {
    class SemaphoreSlimPC0 {
        private readonly int maxPermits;
        private int permits;
        private object monitor;
        private LinkedList<Request> requests;

        private class Request : TaskCompletionSource<bool> {}

        public SemaphoreSlimPC0(int initial, int maxPermits) {
            permits = initial;
            this.maxPermits = maxPermits;
            requests = new LinkedList<Request>();
            monitor = new object();
        }

        public Task<bool> AcquireAsync() {
            lock (monitor) {
                if (permits > 0) {
                    permits--;
                    return Task.FromResult(true);
                }

                Request r = new Request();
                requests.AddLast(r);
               
                return r.Task;
            }
        }

        public bool Acquire() {
            Task<bool> waitTask = AcquireAsync();
            try {
                return waitTask.Result;
            }
            catch (AggregateException ae) {
                throw ae.InnerException;
            }
        }
 
        public void Release(int units) {

            lock (monitor) {
                permits += units;

                while (permits > 0 && requests.Count > 0) {
                    Request r = requests.First.Value;
                    requests.RemoveFirst();
                    
                    permits--;
                    r.SetResult(true);      
                }    
            }
        }

        public void Release() {
            Release(1);
        }
    }
}
