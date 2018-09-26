/*
 * A simple semaphore implemented as a monitor
 * Jorge Martins, september 2019
 */


using System;
using System.Threading;

namespace Aula_2018_09_19 {
    public class Semaphore0 {
        private Object monitor;
        private int permits;

        public Semaphore0(int initialPermits) {
            if (initialPermits >= 0)
                permits = initialPermits;
            monitor = new Object();
        }
        public void Acquire(long timeout) {
            lock(monitor) {
                while(permits == 0)
                   Monitor.Wait(monitor);
                permits--;
            }
        }

        public void Release() {
            lock(monitor) {
                permits++;
                Monitor.Pulse(monitor);
            }
        }
    }
}
