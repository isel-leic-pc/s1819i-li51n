package isel.leic.pc.monitors;

import isel.leic.pc.utils.LinkedList;
import isel.leic.pc.utils.TimeoutHolder;

/**
 * Synchronizer with a semantic similar to windows auto reset event,
 * augmented with a pulseAll operation
 *
 * We use delegation execution techique to avoid barging problems on pulseALl
 * and guarantee FIFO semantic
 */
public class AutoResetEvt {
    private boolean signaled;
    private Object monitor;

    private class Request {
        boolean granted;
    }


    private LinkedList<Request> waiters;


    public AutoResetEvt(boolean initial) {
        signaled = initial;
        monitor = new Object();
        waiters = new LinkedList<>();
    }

    public boolean await(long timeout) throws InterruptedException {
        synchronized(monitor) {
            // non blocking path
            if (signaled) {
                signaled = false; return true;
            }
            if (timeout == 0) return false;
            TimeoutHolder th = new TimeoutHolder(timeout);
            LinkedList.Node<Request> node =  waiters.add(new Request());

            try {
                do {
                    monitor.wait(timeout);
                    if (node.value.granted) return true;
                    if (th.timeout()) {
                        waiters.remove(node);
                        return false;
                    }
                }while(true);
            }
            catch(InterruptedException e) {
                if (node.value.granted) {
                    Thread.currentThread().interrupt();
                    return true;
                }
                waiters.remove(node);
                throw e;
            }

        }
    }

    public void signal() {
        synchronized (monitor) {
            if (waiters.size() == 0)
                signaled = true;
            else {
                Request r = waiters.removeFirst();
                r.granted = true;
                r.notifyAll();
            }
        }
    }

    public void pulseAll() {
        synchronized (monitor) {
            for(Request r : waiters) r.granted=true;
            waiters.clear();
            monitor.notifyAll();
        }
    }
}
