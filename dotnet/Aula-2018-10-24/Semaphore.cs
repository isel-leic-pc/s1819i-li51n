
/*
 * A simple semaphore implemented as a monitor
 * Jorge Martins, september 2019
 */


using SynchUtils;
using System;
using System.Threading;

namespace Aula_2018_10_24 {
    public class Semaphore {
        private Object monitor;
        private volatile int permits;
        private volatile int waiters;

        private bool TryAcquire() {
            /*
            if (permits > 0) {
                --permits;
                return true;
            }
            return false;
            */
            int observed;
            while(true) {
                observed = permits;
                if (observed == 0) return false;
                if (Interlocked.CompareExchange(
                    ref permits, observed - 1, observed) == observed)
                    return true;
            }
        }

        public Semaphore(int initialPermits) {
            if (initialPermits >= 0)
                permits = initialPermits;
            monitor = new Object();
        }

        public bool Acquire(int timeout) {
            if (TryAcquire()) return true;
            if (timeout == 0) return false;
            TimeoutHolder th = new TimeoutHolder(timeout);
           
            lock (monitor) {
                waiters++;

                try {
                    if (TryAcquire()) return true;
                          
                    while (true) {
                        Monitor.Wait(th.Value);
                        if (TryAcquire()) {
                             
                            return true;
                        }
                        if (th.Timeout) {
                             return false;
                        }
                    }
                }
                catch(ThreadInterruptedException) {
                    if (permits > 0)
                        Monitor.Pulse(monitor);
                    throw;
                }
                finally {
                    waiters--;
                }
                
            }   
        }

        public void Release() {
            Interlocked.Increment(ref permits);
            if (waiters >  0) {
                lock (monitor) {
                    if (waiters > 0)
                        Monitor.Pulse(monitor);
                }
            }
            
        }
    }
}
