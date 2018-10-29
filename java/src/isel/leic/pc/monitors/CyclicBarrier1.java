package isel.leic.pc.monitors;

import isel.leic.pc.utils.LinkedList;
import isel.leic.pc.utils.TimeoutHolder;

import java.util.concurrent.BrokenBarrierException;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;
import java.util.concurrent.locks.Condition;
import java.util.concurrent.locks.ReentrantLock;

/**
 * Simple CyclicBarrier that permits particpant exists
 * Done with batch notification technique
 */
public class CyclicBarrier1 {
    private ReentrantLock monitor;
    private Condition phaseCompleted;
    private int participants;
    private int joined;

    private static class RequestBatch  {
        private int current;
        private int size;

        public RequestBatch() {
            current = 0;
            size=0;
        }

        public int add() {
            size++;
            return current;
        }

        public void remove(int current) {
            if (this.current != current || size ==0)
                throw new IllegalStateException();
            size--;
        }

        public int current() {
            return current;
        }

        public void newBatch() {
            current++;
            size=0;
        }
    }

    private RequestBatch batch;

    public CyclicBarrier1(int participants) {
        monitor = new ReentrantLock();
        phaseCompleted = monitor.newCondition();
        joined = 0;
        this.participants = participants;
        batch = new RequestBatch();
    }

    private void openBarrier () {
        joined = 0;
        batch.newBatch();
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
                return 0;
            }
            if (timeout == 0)
                throw new TimeoutException();

            TimeoutHolder th = new TimeoutHolder(timeout);
            int current = batch.add();
            try {
                do {
                    phaseCompleted.await(th.value(), TimeUnit.MILLISECONDS);
                    if (batch.current() != current) return index;
                    if (th.timeout()) {
                        batch.remove(current);
                        throw new TimeoutException();
                    }
                }
                while(true);
            }
            catch(InterruptedException e) {
                if (batch.current() != current) {
                    Thread.currentThread().interrupt();
                    return index;
                }
                batch.remove(current);
                throw e;
            }
        }
        finally {
            monitor.unlock();
        }
    }
}
