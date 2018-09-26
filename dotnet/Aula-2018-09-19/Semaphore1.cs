using SynchUtils;
using System;

using System.Threading;

namespace Aula_2018_09_19 {
    public class Semaphore1 {
        private Object monitor;
        private int permits;

        public Semaphore1(int initialPermits) {
            if (initialPermits >= 0)
                permits = initialPermits;
            monitor = new Object();
        }

        public bool Acquire(int timeout) { // throws InterruptedException 
            lock (monitor) {
                // non blocking path
                if (permits > 0) {
                    --permits;
                    return true;
                }
                if (timeout == 0)
                    return false;
                // blocking path
                TimeoutHolder th = new TimeoutHolder(timeout);
                try {
                    do {
                        Monitor.Wait(th.Value);
                        if (permits > 0) {
                            --permits;
                            return true;
                        }
                        if (th.Timeout) {
                            return false;
                        }
                    }
                    while (true);
                }
                catch (ThreadInterruptedException e) {
                    if (permits > 0)
                        Monitor.Pulse(monitor);
                    throw e;
                }
            }
        }


        public void Release() {
            lock (monitor) {
                ++permits;
                Monitor.Pulse(monitor);
            }
        }
    }
}
