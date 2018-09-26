package isel.leic.pc.tests;

import isel.leic.pc.monitors.CounterSemaphoreED;
import static org.junit.Assert.*;
import org.junit.Test;


public class CounterSemaphoreTestsED {
    @Test
    public void nonBlockingAcquireTest() throws InterruptedException {
        CounterSemaphoreED sem = new CounterSemaphoreED(10);
        boolean result = sem.acquire(5, 1000);
        assertEquals(true, result);
    }

    @Test
    public void immediateFailureAcquireTest() throws InterruptedException {
        CounterSemaphoreED sem = new CounterSemaphoreED(1);
        boolean result = sem.acquire(5, 0);
        assertEquals(false, result);
    }

    @Test
    public void failureByTimeoutAcquireTest() throws InterruptedException {
        CounterSemaphoreED sem = new CounterSemaphoreED(1);
        boolean[] result = new boolean[1];

        Thread t = new Thread(() -> {
            try {
                boolean res = sem.acquire(5, 1000);
                result[0] = !res;
            }
            catch(InterruptedException e) {

            }
        });
        t.start();

        // wait for thread termination with timeout
        // in order to made the test robust
        t.join(5000);
        if (t.isAlive()) { t.interrupt(); fail(); }
        assertEquals(true, result[0]);
    }

    @Test
    public void multipleAcquireTest() throws InterruptedException {
        CounterSemaphoreED sem = new CounterSemaphoreED(10);
        boolean[] result = new boolean[2];


        Thread t1 = new Thread(() -> {
            try {
                result[0]= sem.acquire(5, 1000);
            } catch (InterruptedException e) {

            }
        });
        Thread t2 = new Thread(() -> {
            try {
                result[1] = sem.acquire(5, 1000);
            } catch (InterruptedException e) {

            }
        });

        t1.start();
        t2.start();

        // wait for thread termination with timeout
        // in order to made the test robust
        t1.join(5000);
        t2.join(5000);
        if (t1.isAlive()) { t1.interrupt(); fail(); }
        if (t2.isAlive()) { t2.interrupt(); fail(); }

        assertEquals(true, result[0]);
        assertEquals(true, result[1]);
    }
}
