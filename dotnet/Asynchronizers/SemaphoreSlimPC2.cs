using AsyncUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Asynchronizers {
    public class SemaphoreSlimPC2 {
        private readonly int maxPermits;
        private volatile int permits;
        private volatile int awaiters;
        private object monitor;

        private class Request {
            internal TaskCompletionSource<bool> promise;
            internal CancellationToken token;
            internal Timer timer;
            internal SemaphoreSlimPC2 sem;
            internal LinkedListNode<Request> node;
            internal int timeout;

            internal Request(SemaphoreSlimPC2 sem, CancellationToken token, int timeout) {
                promise = new TaskCompletionSource<bool>();
                this.token = token;
                this.sem = sem;
                this.timeout = timeout;
            }

            internal void Init(LinkedListNode<Request> node) {
                this.node = node;
                timer = new Timer((o) => sem.TimeoutCallback(node));
            }

            internal Task<bool> Start() {
                token.Register(() => sem.CancellationCallback(node));
                timer.Change(timeout, Timeout.Infinite);
                return promise.Task;
            }
        }

        LinkedList<Request> requests;

        public SemaphoreSlimPC2(int initial, int maxPermits) {
            permits = initial;
            this.maxPermits = maxPermits;
            requests = new LinkedList<Request>();
            monitor = new object();
        }

        private void CancellationCallback(LinkedListNode<Request> node) {
            Request r = node.Value;
            if (r.promise.TrySetCanceled()) {
                r.timer.Dispose();
                lock (monitor) requests.Remove(node);
            }
        }

        private void TimeoutCallback(LinkedListNode<Request> node) {
            Request r = node.Value;
            if (r.promise.TrySetResult(false)) {
                lock (monitor) requests.Remove(node);
            }
        }

        private bool TryAcquire() {
            while (true) {
                int obsPermits = permits;
                if (obsPermits == 0) return false;
                if (Interlocked.CompareExchange(ref permits, obsPermits - 1, obsPermits)
                    == obsPermits)
                    return true;
            }
        }

        private void AddPermits(int units) {
            while (true) {
                int obsPermits = permits;
                if (obsPermits + units > maxPermits)
                    throw new InvalidOperationException();
                if (Interlocked.CompareExchange(ref permits, obsPermits + units, obsPermits)
                    == obsPermits)
                    return;
            }
        }


        public Task<bool> AcquireAsync(CancellationToken token, int timeout) {
            if (token.IsCancellationRequested)
                return Task.FromCanceled<bool>(token);

            if (TryAcquire())
                return Task.FromResult(true);
            if (timeout == 0)
                return Task.FromResult(false);
            Request r = null;
            LinkedListNode<Request> node = null;
            lock (requests) {
                if (token.IsCancellationRequested)
                    return Task.FromCanceled<bool>(token);
                r = new Request(this, token, timeout);
             
                awaiters++;
                if (TryAcquire()) {
                    awaiters--;
                    return Task.FromResult(true);
                }
                node = requests.AddLast(r);

                r.Init(node);
            }
            return r.Start();
            /*
             *    r.RegistCancel(() => CancellationCallback(node));

            return Task.WhenAny(r.promise.Task, Task.Delay(timeout)).
              ContinueWith(ant => {
                  Task t = ant.Result;
                  if (t == r.promise.Task) {
                      if (t.IsCanceled) throw new OperationCanceledException(token);
                      else if (t.IsFaulted) throw t.Exception;
                      else return r.promise.Task.Result;
                  }
                  else {
                      var exc = new TimeoutException()
                      if (r.promise.TrySetException(exc))
                          lock (requests) requests.Remove(node);
                      throw exc;
                  }
              });
             

            return r.promise.Task.WithTimeout(timeout, () => {
                if (r.promise.TrySetException(new TimeoutException()))
                    lock (monitor) requests.Remove(node);
            });
            */

        }

        public bool Acquire(CancellationToken token, int timeout) {
            Task<bool> t = AcquireAsync(token, timeout);
            try {
                t.Wait();
                return t.Result;
            }
            catch (AggregateException e) {
                Exception inner = e.Flatten().InnerException;
                throw inner;
            }

        }

        public void Release(int units) {
            AddPermits(units);
            if (awaiters > 0) {
                lock (monitor) {
                    LinkedListNode<Request> node = requests.First;
                    while (node != null && TryAcquire()) {
                        var current = node;
                        node = node.Next;
                        if (node.Value.promise.TrySetResult(true)) {
                            requests.Remove(current);
                            awaiters--;
                        }
                        else AddPermits(1);
                       
                    }
                }
            }
        }

        public void Release() {
            Release(1);
        }
    }
}
