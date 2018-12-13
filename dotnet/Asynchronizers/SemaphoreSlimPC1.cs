using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Asynchronizers {

    /// <summary>
    /// A semaphore with async acquire operation.
    /// This version can be reentered by synchronous continuations without any problem.
    /// </summary>
    class SemaphoreSlimPC1 {
        private readonly int maxPermits;
        private int permits;
        private object monitor;
        private LinkedList<Request> requests; // the asynchronous pending requests

        /// <summary>
        /// The request nodes are augmented promises (TaskCompletionSource) 
        /// with additional state
        /// </summary>
        private class Request : TaskCompletionSource<bool> {
            internal Timer timer;
            internal SemaphoreSlimPC1 sem;
            internal CancellationTokenRegistration cancelRegist;
            internal CancellationToken token;
            internal LinkedListNode<Request> node;
            internal Request(SemaphoreSlimPC1 sem) {
                this.sem = sem;  
            }

            internal Task<bool> Start(LinkedListNode<Request> node, CancellationToken token, int timeout) {
                this.token = token;
                this.node = node;
                // register a cancellation callback if the token is cancellable, i.e., is not CancellationToken.None
                if (token.CanBeCanceled)
                    // Note that if the token is already cancelled, the callback is called synchronously!
                    cancelRegist = token.Register(() => sem.CancellationCallback(node));

                // Create a timer if timeout is not infinite 
                // and the task can be already completed due to a premature cancellation
                if (timeout != Timeout.Infinite && !Task.IsCompleted) 
                    timer = new Timer((o) => sem.TimeoutCallback(node), null, timeout, Timeout.Infinite);

                // return the promise associated task
                return Task;
            }
        }

        public SemaphoreSlimPC1(int initial, int maxPermits) {
            permits = initial;
            this.maxPermits = maxPermits;
            requests = new LinkedList<Request>();
            monitor = new object();
        }

        /// <summary>
        /// A try to put the promise in cancelled final state
        /// Note that the semaphore can be reentered in a synchronous continuation that
        /// again use the semaphore! We must be very carefull about this situation.
        /// </summary>
        /// <param name="node"></param>
        private void CancellationCallback(LinkedListNode<Request> node) {
          
            Request r = null;
            lock (monitor) {
                if (node.List == requests) {// we must check if the node still is in the list!    
                    // ok, "terminate" the node, then
                    r = node.Value;
                    requests.Remove(node);  // first remove the node   
                  
                   
                }
               
            }
            if (r!= null) {
                r.timer?.Dispose();     // dispose the timer if there is one
                r.SetCanceled();// then force final task state outside the lock 
            }
          

        }

        /// <summary>
        /// A try to put the promise in a false result (meaning timeout) final state.
        /// Note that the semaphore can be reentered in a synchronous continuation that
        /// again use the semaphore! We must be very carefull about this situation.
        /// </summary>
        /// <param name="node"></param>
        private void TimeoutCallback(LinkedListNode<Request> node) {
           
            Request r = null;
            lock (monitor) {
                if (node.List == requests) {  // we must check if the node still is in the list! 
                    // ok, "terminate" the node, then
                    r = node.Value;
                    requests.Remove(node);  // remove the node 
                  
                   
                }
               
            }
            if (r!= null) {
                if (r.token.CanBeCanceled)
                    r.cancelRegist.Dispose();
                r.SetResult(false);     // force final task state outside the lock 
            }
          

        }

        /// <summary>
        /// Async acquires on a semaphore unit. If there are available units,
        /// the operation is already cancelled or there is an immediate timeout, we return synchronously 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public Task<bool> AcquireAsync(CancellationToken token, int timeout) {
            Request r = null;
            LinkedListNode<Request> node = null;

            lock (monitor) {
                if (permits > 0) {
                    permits--;
                    return Task.FromResult(true);
                }

                if (timeout == 0)
                    return Task.FromResult(false);

                if (token.IsCancellationRequested)
                    return Task.FromCanceled<bool>(token);

                // add a new request to pending requests
                r = new Request(this);
                node = requests.AddLast(r);
                return r.Start(node, token, timeout);

            }  

        }


        // Try to cancel an asynchronous request identified by its task
        public bool TryCancelAcquire(Task<bool> requestTask) {
            Request request = null;
            lock (monitor) {
                foreach (Request req in requests) {
                    if (req.Task == requestTask) {
                        request = req;

                        requests.Remove(req);

                        break;
                    }
                }
            }
            if (request != null) {
                request.timer?.Dispose();
                if (request.token.CanBeCanceled) {
                    request.SetCanceled();
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Synchronous acquire version. 
        /// Is done invoking the asynchronous version and immediately waiting on that one
        /// </summary>
        /// <param name="token"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool Acquire(CancellationToken token, int timeout) {
            Task<bool> waitTask = AcquireAsync(token, timeout);
            try {
                return waitTask.Result;
            }
            catch (ThreadInterruptedException) {
                // Try to cancel the asynchronous request
                if (TryCancelAcquire(waitTask))
                    throw;
                // The request was already completed or cancelled, return the
                // underlying result
                try {
                    return waitTask.Result;
                }
                catch (AggregateException ae) {
                    throw ae.InnerException;
                }
                finally {
                    // Anyway re-assert the interrupt
                    Thread.CurrentThread.Interrupt();
                }
            }
            catch (AggregateException ae) {
                throw ae.InnerException;
            }
        }

        /// <summary>
        /// Give the semaphore a units ammount, eventually complete waiting requests.
        /// Note that the semaphore can be reentered in a synchronous continuation that
        /// again use the semaphore! We must be very carefull about this situation.
        /// </summary>
        /// <param name="units"></param>
        public void Release(int units) {
            LinkedList<Request> awaken = new LinkedList<Request>();
            lock (monitor) {
                //if (permits + units < permits || permits + units > maxPermits)
                //    throw new InvalidOperationException();
                permits += units;

                while (requests.Count > 0 && permits > 0) {
                    Request r = requests.First.Value;
                    requests.RemoveFirst();
                    // Copy the request to a local list to avoid lock reentrancy!
                    awaken.AddLast(r);
                    permits--;
                   
                }
            }

            // finally complete the pending requests
            foreach (Request r in awaken) {
                r.timer?.Dispose();
                if (r.token.CanBeCanceled) r.cancelRegist.Dispose();
                r.SetResult(true);
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
