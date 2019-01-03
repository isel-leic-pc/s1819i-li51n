using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Asynchronizers {
    class MutexAsync {

        private class Request : TaskCompletionSource<bool> {
            internal const int OK = 1;
            internal const int USED = 2;
            internal const int DELETED = 3;

            internal Timer timer;
            internal MutexAsync mtx;
            internal CancellationTokenRegistration cancelRegist;
            internal CancellationToken token;
            internal volatile int state;

            internal Request(MutexAsync mtx) {
                this.mtx = mtx;
                state = OK;
            }

            internal Task<bool> Start( int timeout, CancellationToken token) {
                this.token = token;

                // register a cancellation callback if the token is cancellable, i.e., is not CancellationToken.None
                if (token.CanBeCanceled)
                    // Note that if the token is already cancelled, the callback is called synchronously!
                    cancelRegist = token.Register(() => mtx.CancellationCallback(this));

                // Create a timer if timeout is not infinite 
                // and the task is not already completed due to a premature cancellation
                if (timeout != Timeout.Infinite && !Task.IsCompleted)
                    timer = new Timer((o) => mtx.TimeoutCallback(this), null, timeout, Timeout.Infinite);

                // return the promise associated task
                return Task;
            }

            internal bool TryOwnership(int finalState) {
                return Interlocked.CompareExchange(ref state, finalState, OK) == OK;
            }

            internal void Dispose() {
                if (timer != null) timer.Dispose();
                if (token.CanBeCanceled) {
                    cancelRegist.Dispose();
                }
            }

            internal bool IsDeleted() {
                return state == DELETED;
            }
        }

        private volatile int owned;     // 1 if the mutex is owned, 0 if not

        // tasks for success and failure of AcquireAsync
        private static Task<bool> taskFalse = Task.FromResult(false);
        private static Task<bool> taskTrue = Task.FromResult(true);

        // the acquire requests queue
        private ConcurrentQueue<Request> requests;

        private volatile int awaiters;
        public MutexAsync(bool initial) {
            requests = new ConcurrentQueue<Request>();
            owned = (initial) ? 1 : 0;
        }

        /// <summary>
        /// A try to put the promise in cancelled final state
        /// Note that the semaphore can be reentered in a synchronous continuation that
        /// again use the semaphore! We must be very carefull about this situation.
        /// </summary>
        /// <param name="node"></param>
        private void CancellationCallback(Request req) {

            if (req.TryOwnership(Request.DELETED)){
                Interlocked.Decrement(ref awaiters);
                req.Dispose();

                req.SetCanceled();
            }
            
        }

        /// <summary>
        /// A try to put the promise in a false result (meaning timeout) final state.
        /// Note that the semaphore can be reentered in a synchronous continuation that
        /// again use the semaphore! We must be very carefull about this situation.
        /// </summary>
        /// <param name="node"></param>
        private void TimeoutCallback(Request req) {
            if (req.TryOwnership(Request.DELETED)) {
                Interlocked.Decrement(ref awaiters);
                req.Dispose();
                req.SetResult(false);
            }

        }

        private bool TryAcquire() {
            return Interlocked.CompareExchange(ref owned, 1, 0) == 0;
        }

        public Task<bool> AcquireAsync(int timeout, CancellationToken token) {
            if (TryAcquire()) return taskTrue;
            if (timeout == 0) return taskFalse;
            Interlocked.Increment(ref awaiters);
            if (TryAcquire()) { Interlocked.Decrement(ref awaiters); return taskTrue; }
            Request r = new Request(this);
            requests.Enqueue(r);
            return r.Start(timeout, token);
        }

        private volatile int inRelease;
        public void Release() {
            if (Interlocked.CompareExchange(ref inRelease, 1, 0) != 0 || owned == 0)
                throw new InvalidOperationException("Release should not be concurrent");
            Request r = null;
            while (awaiters > 0) {  
                if (requests.TryDequeue(out r)) {
                    if (!r.TryOwnership(Request.USED)) {
                         r = null; continue; 
                    }
                    else {
                        Interlocked.Decrement(ref awaiters);
                        break;
                    }
                }
            }
            inRelease = 0;
            if (r == null) owned = 0;
            else r.SetResult(true); 
        }
    }
}
