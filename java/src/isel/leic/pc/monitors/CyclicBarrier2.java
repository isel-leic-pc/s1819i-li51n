package isel.leic.pc.monitors;

import isel.leic.pc.utils.LinkedList;
import isel.leic.pc.utils.TimeoutHolder;

import java.util.concurrent.BrokenBarrierException;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;
import java.util.concurrent.locks.Condition;
import java.util.concurrent.locks.ReentrantLock;

/**
 * CyclicBarrier that does not permits participant exits.
 * If someone exit, the barrier enters in a (terminal) broken state
 * Done with execution delegation based on the waiters (participants) list.
 */
public class CyclicBarrier2 {
    private ReentrantLock monitor;
    Condition phaseCompleted;
    private int participants;
    private int joined;
    private LinkedList<Waiter> waiters;

    private enum State { Closed, Broken, Opened };

    private boolean broken;

    private static class Waiter {
        private State state;

        private Waiter() {
            state = State.Closed;
        }
    }

    public CyclicBarrier2(int participants) {
        monitor = new ReentrantLock();
        phaseCompleted = monitor.newCondition();
        joined = 0;
        this.participants = participants;
        waiters = new LinkedList<>();
        broken = false;
    }

    private void concludePhase (State newState) {
        while(waiters.size() > 0) {
            Waiter w = waiters.removeFirst();
            w.state = newState;
        }
        broken = newState == State.Broken;
        phaseCompleted.signalAll();
    }



    public int await(long timeout)
            throws InterruptedException, TimeoutException, BrokenBarrierException {
        monitor.lock();
        try {
            if (broken)
                throw new BrokenBarrierException();
            int index = ++joined;
            if (index == participants) {
                // last participant arrived, lets wakeup the others
                concludePhase(State.Opened);
                return 0;
            }
            if (timeout == 0) {
                concludePhase(State.Broken);
                throw new TimeoutException();
            }
            TimeoutHolder th = new TimeoutHolder(timeout);
            Waiter w = new Waiter();
            try {
                do {
                    phaseCompleted.await(th.value(), TimeUnit.MILLISECONDS);
                    if (w.state == State.Opened) return index;
                    if (w.state == State.Broken) {
                        throw new BrokenBarrierException();
                    }
                    if (th.timeout()) {
                        concludePhase(State.Broken);
                        throw new TimeoutException();
                    }
                }
                while(true);
            }
            catch(InterruptedException e) {
                if (w.state == State.Opened) {
                    Thread.currentThread().interrupt();
                    return index;
                }
                if (w.state == State.Closed) {
                    concludePhase(State.Broken);
                }
                throw e;
            }
        }
        finally {
            monitor.unlock();
        }
    }
}
