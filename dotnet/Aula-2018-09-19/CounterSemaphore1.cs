using SynchUtils;
using System;

using System.Threading;

namespace Aula_2018_09_19 {
    public class CounterSemaphore1 {
        private Object monitor;
        private int permits;

        public CounterSemaphore1(int initialPermits) {
            if (initialPermits >= 0)
                permits = initialPermits;
            monitor = new Object();
        }

        public bool Acquire(int requests, int timeout) { // throws InterruptedException  
            lock (monitor) {
                // non blocking path
                if (permits >= requests) {
                    permits -= requests;
                    return true;
                }
                if (timeout == 0)
                    return false;
                // blocking path
                TimeoutHolder th = new TimeoutHolder(timeout);
                try {
                    do {
                        Monitor.Wait(monitor, th.Value);
                        if (permits >= requests) {
                            permits -= requests;
                            return true;
                        }
                        if (th.Timeout) return false;
                    }
                    while (true);
                }
                catch (ThreadInterruptedException e) {
                    // In this case no exception treatment is necessary
                    // the catch clausule is redundant
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
