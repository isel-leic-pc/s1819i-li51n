/*--------------------------------------------------------------
 * counter semaphore implementation with execution delegation
 */

package isel.leic.pc.monitors;

import isel.leic.pc.utils.TimeoutHolder;


// We use our LinkedList that returns the node references for added items,
// in order to optimize node remove implementation
import isel.leic.pc.utils.LinkedList;


public class CounterSemaphoreED {
    private Object monitor;
    private int permits;

    private static class Request {
        final int n;
        boolean granted;
        Request(int n) { this.n = n;}
    }

    private LinkedList<Request> waiters;

    public CounterSemaphoreED(int initialPermits) {
        if (initialPermits >=0)
            permits = initialPermits;
        monitor = new Object();
        waiters = new LinkedList<>();
    }

    private void notifyWaiters() {
        int waken = 0;
        while (waiters.size() > 0 && permits >= waiters.getFirst().n) {
            Request req = waiters.removeFirst();
            req.granted = true;
            permits -= req.n;
            waken++;
        }
        if (waken > 0) monitor.notifyAll();
    }

    public boolean acquire(int n, long timeout) throws InterruptedException {
        synchronized(monitor) {
            // Here is the correct test to do.
            // On the lecture, the test for empty waiters list was forgotten,
            // which permits barging, that is, is possible that an
            // acquiring passes in front of waiting threads, jeopardizing FIFO discipline!
            if (permits >= n && waiters.size() == 0) {
                permits -= n;
                return true;
            }
            if (timeout == 0) return false;
            TimeoutHolder th = new TimeoutHolder(timeout);
            Request req = new Request(n);
            LinkedList.Node node = waiters.add(req);

            try {
                do {
                    monitor.wait(th.value());
                    // on execution delegation pattern, just check n was granted
                    if (req.granted)
                        return true;

                    if (th.timeout() ) {
                        waiters.remove(node);
                        notifyWaiters();
                        return false;
                    }
                }
                while (true);
            }
            catch(InterruptedException e) {
                if (req.granted) {
                    // on execution delegation pattern, in case of n was granted
                    // just delay the interrupt and return success
                    Thread.currentThread().interrupt();
                    return true;
                }
                waiters.remove(node);
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
