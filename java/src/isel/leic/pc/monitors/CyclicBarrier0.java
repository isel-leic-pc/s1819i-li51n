package isel.leic.pc.monitors;

import isel.leic.pc.utils.LinkedList;
import isel.leic.pc.utils.TimeoutHolder;

import java.util.concurrent.BrokenBarrierException;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;
import java.util.concurrent.locks.Condition;
import java.util.concurrent.locks.ReentrantLock;

public class CyclicBarrier0 {
    private ReentrantLock monitor;
    Condition phaseCompleted;

    private int participants;
    private int joined;
    private LinkedList<Request> waiters;

    private static class Request {
        private boolean granted;
    }

    public CyclicBarrier0(int participants) {
        monitor = new ReentrantLock();
        phaseCompleted = monitor.newCondition();
        joined = 0;
        this.participants = participants;
        waiters = new LinkedList<>();
    }

    private void openBarrier() {
        while(waiters.size() > 0) {
            Request r = waiters.removeFirst();
            r.granted = true;
        }
        joined = 0;
        phaseCompleted.signalAll();
    }



    public int await(long timeout)
            throws InterruptedException, TimeoutException, BrokenBarrierException {
        monitor.lock();
        try {

            int index = ++joined;
            if (index == participants) {
                // last participant arrived, lets wakeup the others
                openBarrier();
                // the opener index is 0
                return 0;
            }
            if (timeout == 0) {
                throw new TimeoutException();
            }
            TimeoutHolder th = new TimeoutHolder(timeout);
            Request req = new Request();
            LinkedList.Node<Request> node = waiters.add(req);
            try {
                do {
                    phaseCompleted.await(th.value(), TimeUnit.MILLISECONDS);
                    if (req.granted) return index;
                    if (th.timeout()) {
                        waiters.remove(node);
                        throw new TimeoutException();
                    }
                }
                while(true);
            }
            catch(InterruptedException e) {
                if (req.granted) {
                    Thread.currentThread().interrupt();
                    return index;
                }
                waiters.remove(node);
                throw e;
            }
        }
        finally {
            monitor.unlock();
        }
    }
}
