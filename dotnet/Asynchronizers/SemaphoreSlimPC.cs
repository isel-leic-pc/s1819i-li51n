using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Asynchronizers {

    /// <summary>
    /// A semaphore with async acquire operation.
    /// This version avoids reentrancy by forbidden synchronous continuations.
    /// </summary>
    class SemaphoreSlimPC {
        private readonly int maxPermits;
        private int permits;
        private object monitor;
        private LinkedList<Request> requests;   // the asynchronous pending requests
        private class Request : TaskCompletionSource<bool> {
            internal CancellationToken token;
            internal Timer timer;
            internal SemaphoreSlimPC sem;
            internal LinkedListNode<Request> node;
            internal int timeout;

            /// The request nodes are augmented promises (TaskCompletionSource) 
            /// with additional state. They are created with a creation option
            /// that forbids synchronous continuations!
            /// </summary>
            internal Request(SemaphoreSlimPC sem, CancellationToken token, int timeout) 
                : base(TaskCreationOptions.RunContinuationsAsynchronously) {
                this.token = token;
                this.sem = sem;
                this.timeout = timeout;
            }
 
            internal Task<bool> Start(LinkedListNode<Request> node) {
                this.node = node; // the node is saved for efficient remotion 

                // Create a timer if timeout is not infinite
                if (timeout != Timeout.Infinite) {
                    timer = new Timer((o) => sem.TimeoutCallback(node));
                    timer.Change(timeout, Timeout.Infinite);
                }

                // register a cancellation callback if the token is cancellable, i.e., is not
                // CancellationToken.None
                if (token.CanBeCanceled)
                    // Note that if the token is already cancelled, the callback is called synchronously!
                    token.Register(() => sem.CancellationCallback(node));
              
                return Task;
            }
        }

       

        public SemaphoreSlimPC(int initial, int maxPermits) {
            permits = initial;
            this.maxPermits = maxPermits;
            requests = new LinkedList<Request>();
            monitor = new object();
        }

        /// <summary>
        /// A try to put the promise in cancelled final state
        /// </summary>
        /// <param name="node"></param>
        private void CancellationCallback(LinkedListNode<Request> node) {
            Request r = node.Value;
            if (r.TrySetCanceled()) {
                r.timer.Dispose();
                lock (monitor) requests.Remove(node);
            }
        }

        /// <summary>
        /// A try to put the promise in a false result (meaning timeout) final state.
        /// </summary>
        /// <param name="node"></param>
        private void TimeoutCallback(LinkedListNode<Request> node) {
            Request r = node.Value;
            if (r.TrySetResult(false)) {
                lock (monitor) requests.Remove(node);
            }
                
        }

        /// <summary>
        /// Async acquires on a semaphore unit. If there are available units,
        /// the operation is already cancelled or there is an immediate timeout, we return synchronously.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public Task<bool> AcquireAsync(CancellationToken token, int timeout) {
            Request r = null;
            LinkedListNode<Request> node = null;

            lock(monitor) {
                if (permits > 0) {
                    permits--;
                    return Task.FromResult(true);
                }
                    
                if (timeout == 0)
                    return Task.FromResult(false);
              
                if (token.IsCancellationRequested)
                    return Task.FromCanceled<bool>(token);

                r = new Request(this, token, timeout);
                node = requests.AddLast(r);
               
                return r.Start(node);
            }
           
        }

        /// <summary>
        /// Synchronous acquire version. 
        /// Is done invoking the asynchronous version and immediately waiting on that one
        /// </summary>
        /// <param name="token"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool Acquire(CancellationToken token, int timeout) {
            Task<bool> waitTask = AcquireAsync( token, timeout);
            try {
                return waitTask.Result;
            }
            catch (AggregateException ae) {
                throw ae.InnerException;
            }
        }

        /// <summary>
        /// Give the semaphore a units ammount, eventually complete waiting requests.
        /// </summary>
        /// <param name="units"></param>
        public void Release(int units) {
            
            lock (monitor) {
                permits += units;

                if (permits > maxPermits)
                    throw new InvalidOperationException();

                LinkedListNode<Request> node = requests.First;
                
                while (node != null && permits>0) {
                    Request r = node.Value;
                    var current = node;
                    node = node.Next;

                    if (r.TrySetResult(true)) {
                        permits--;
                        requests.Remove(current);
                        r.timer.Dispose();
                    } 
                }        
            }
        }

        /// <summary>
        /// Overload to simplify 1 unit release
        /// </summary>
        public void Release() {
            Release(1);
        }
    }
}
