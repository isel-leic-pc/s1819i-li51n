using SynchUtils;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Aula_2018_09_25 {
    public class CounterSemaphoreFIFO {
        private Object monitor;
        private int permits;
        LinkedList<int> waiters;

        public CounterSemaphoreFIFO(int initialPermits) {
            if (initialPermits >= 0)
                permits = initialPermits;
            monitor = new Object();
            waiters = new LinkedList<int>();
        }

        private void notifyWaiters() {
            if (waiters.Count > 0 && permits >= waiters.First.Value)  
                Monitor.PulseAll(monitor);
        }

        public bool Acquire(int requests, int timeout) { // throws InterruptedException  
            lock (monitor) {
                // non blocking path
                if (permits >= requests && waiters.Count > 0) {
                    permits -= requests;
                    return true;
                }
                if (timeout == 0)
                    return false;
                // blocking path
                // prepare wait
                TimeoutHolder th = new TimeoutHolder(timeout);
                var node = waiters.AddLast(requests);
                try {
                    do {
                        Monitor.Wait(monitor, th.Value);
                        if (waiters.First == node &&  permits >= requests) {
                            waiters.RemoveFirst();
                            permits -= requests;
                            notifyWaiters();
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
                    waiters.Remove(node);
                    notifyWaiters();
                    throw e;
                }
            }
        }

        public void Release(int releases) {
            lock (monitor) {
                permits += releases;
                Monitor.PulseAll(monitor);
            }
        }
    }
}
