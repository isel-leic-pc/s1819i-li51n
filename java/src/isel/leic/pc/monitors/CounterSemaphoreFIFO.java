package isel.leic.pc.monitors;

import isel.leic.pc.utils.TimeoutHolder;

import java.util.LinkedList;


public class CounterSemaphoreFIFO {
    private Object monitor;
    private int permits;

    private static class Request {
        private final int n;
        private Request(int n) { this.n = n;}
    }
    private LinkedList<Request> waiters;

    public CounterSemaphoreFIFO(int initialPermits) {
        if (initialPermits >=0)
            permits = initialPermits;
        monitor = new Object();
        waiters = new LinkedList<>();
    }

    private void notifyWaiters() {
        if (waiters.size() > 0 && permits >= waiters.getFirst().n)
            monitor.notifyAll();
    }

    public boolean acquire(int n, long timeout) throws InterruptedException {
        synchronized(monitor) {
            // Here is the correct test to do.
            // On the lecture, the test for empty waiters list was forgotten,
            // which permits barging, that is, is possible that an
            // acquiring thread passes in front of waiting threads,
            // jeopardizing FIFO discipline!
            if (permits >= n && waiters.size() == 0) {
                permits -= n;
                return true;
            }
            if (timeout == 0) return false;
            TimeoutHolder th = new TimeoutHolder(timeout);
            Request req = new Request(n);
            waiters.add(req);

            try {
                do {
                    monitor.wait(th.value());
                    if (waiters.getFirst() == req && permits >= n) {
                        waiters.removeFirst();
                        permits -= n;
                        notifyWaiters();
                        return true;
                    }
                    if (th.value() == 0) {
                        waiters.remove(req);
                        notifyWaiters();
                        return false;
                    }
                }
                while (true);
            }
            catch(InterruptedException e) {
                waiters.remove(req);
                notifyWaiters();
                throw e;
            }
        }
    }

    public void release(int n) {
        synchronized (monitor) {
            permits += n;
            notifyWaiters();
        }
    }

}
