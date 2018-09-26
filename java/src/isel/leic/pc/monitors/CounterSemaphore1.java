/*
 * Simple implementation for counter semaphore.
 * This implementation is unfair for clients with higher requests
 * the can be starved by clients with smaller requests.
 * Additionally, barging is present
 */

package isel.leic.pc.monitors;

import isel.leic.pc.utils.TimeoutHolder;

public class CounterSemaphore1 {
    private Object monitor;
    private int permits;

    public CounterSemaphore1(int initialPermits) {
        if (initialPermits >=0)
            permits = initialPermits;
        monitor = new Object();
    }

    public boolean acquire(int requests, long timeout) throws InterruptedException{
        synchronized (monitor) {
            // non blocking path
            if (permits >= requests) {
                permits -= requests;
                return true;
            }
            if (timeout ==0)
                return false;

            // blocking path
            TimeoutHolder th = new TimeoutHolder(timeout);

            do {
                monitor.wait(th.value());
                if (permits >= requests) {
                    permits -= requests;
                    return true;
                }
                if (th.timeout()) return false;
            }
            while(true);
        }
    }

    public void release(int releases) {
        synchronized (monitor) {
            permits += releases;
            monitor.notifyAll();
        }
    }
}
