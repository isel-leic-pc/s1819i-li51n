package isel.leic.pc.monitors;

import isel.leic.pc.utils.TimeoutHolder;

import java.sql.Timestamp;

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
            try {
                do {
                    monitor.wait(th.value());
                    if (permits >= requests) {
                        permits -= requests;
                        return true;
                    }
                    if (th.value() == 0) return false;
                }
                while(true);
            }
            catch(InterruptedException e) {
                throw e;
            }
        }
    }

    public void release(int releases) {
        synchronized (monitor) {
            permits += releases;
            monitor.notifyAll();
        }
    }
}
