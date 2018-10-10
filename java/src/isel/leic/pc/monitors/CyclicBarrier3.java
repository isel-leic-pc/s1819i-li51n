package isel.leic.pc.monitors;

import isel.leic.pc.utils.GenericBatch;
import isel.leic.pc.utils.LinkedList;
import isel.leic.pc.utils.TimeoutHolder;

import java.util.concurrent.BrokenBarrierException;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;
import java.util.concurrent.locks.Condition;
import java.util.concurrent.locks.ReentrantLock;

public class CyclicBarrier3 {

    private enum State { Closed, Broken, Opened };


    private ReentrantLock monitor;
    private Condition phaseCompleted;
    private int participants;
    private int joined;
    private GenericBatch<State> batch;
    private boolean broken;
    private Runnable action;

    public CyclicBarrier3(int participants, Runnable action) {
        monitor = new ReentrantLock();
        phaseCompleted = monitor.newCondition();
        joined = 0;
        this.participants = participants;
        batch = new GenericBatch<>(State.Closed);
        broken = false;
        this.action = action;
    }

    private void completePhase(State newState) {
        if (broken) return;
        joined = 0;
        broken = newState == State.Broken;
        batch.current().value = newState;
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
                boolean runSucceeded = false;
                try {
                    if (action != null)
                        action.run();
                    runSucceeded = true;
                }
                finally {
                    if (runSucceeded) {
                        // last participant arrived, lets wakeup the others
                        completePhase(State.Opened);
                    }
                    else {
                        // propagate action error breaking barrier
                        completePhase(State.Broken);
                    }
                }

                return 0;
            }
            if (timeout == 0) {
                completePhase(State.Broken);
                throw new TimeoutException();
            }
            TimeoutHolder th = new TimeoutHolder(timeout);
            GenericBatch.Request<State> current = batch.add();
            try {
                do {
                    phaseCompleted.await(th.value(), TimeUnit.MILLISECONDS);
                    if (current.value  == State.Opened) return index;
                    if (current.value  == State.Broken) {
                        throw new BrokenBarrierException();
                    }
                    if (th.timeout()) {
                        completePhase(State.Broken);
                        throw new TimeoutException();
                    }
                }
                while(true);
            }
            catch(InterruptedException e) {
                if (current.value == State.Opened) {
                    Thread.currentThread().interrupt();
                    return index;
                }

                completePhase(State.Broken);
                throw e;
            }
        }
        finally {
            monitor.unlock();
        }
    }
}
