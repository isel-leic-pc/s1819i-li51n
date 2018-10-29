using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SynchUtils;

namespace Aula_2018_10_09 {
    /// <summary>
    /// Semaphore implementation using execution delegation
    /// and specific notification to minimize context switches
    /// </summary>
    public class CounterSemaphoreSN {
        private Object monitor;
        private int permits;
        LinkedList<Request> waiters;

        private class Request {
            internal int reqUnits;
            internal bool granted;

            internal Request(int reqUnits) {
                this.reqUnits = reqUnits;
            }
        }

        public CounterSemaphoreSN(int initialPermits) {
            if (initialPermits < 0)
                throw new InvalidOperationException();

            permits = initialPermits;
            monitor = new Object();
            waiters = new LinkedList<Request>();
        }

        private void notifyWaiters() {
            while (waiters.Count > 0) {
                LinkedListNode<Request> n = waiters.First;
                if (n.Value.reqUnits > permits) return;
                permits -= n.Value.reqUnits;
                n.Value.granted = true;
                waiters.RemoveFirst();
                //thewaiters node is the condition!
                monitor.Notify(n);
            }
             
        }

        public bool Acquire(int units, int timeout) { // throws InterruptedException  
            lock (monitor) {
                // non blocking path
                if (permits >= units && waiters.Count == 0) {
                    permits -= units;
                    return true;
                }
                if (timeout == 0)
                    return false;
                // blocking path
                // prepare wait
                TimeoutHolder th = new TimeoutHolder(timeout);
                var node = waiters.AddLast(new Request(units));
              
                try {
                    do {
                        // use waiters node as a new condition!
                        monitor.Wait(node, th.Value);
                        if (node.Value.granted) {
                            return true;
                        }
                        if (th.Timeout) {
                            waiters.Remove(node);
                            notifyWaiters();
                            return false;
                        }
                    }
                    while (true);
                }
                catch (ThreadInterruptedException e) {
                    if (node.Value.granted) {
                        Thread.CurrentThread.Interrupt();
                        return true;
                    }
                    waiters.Remove(node);
                    notifyWaiters();
                    throw e;
                }
            }
        }

        public void Release(int releases) {
            lock (monitor) {
                permits += releases;
                notifyWaiters();
            }
        }
    }
}
