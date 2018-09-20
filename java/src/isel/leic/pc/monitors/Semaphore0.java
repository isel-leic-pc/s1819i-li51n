package isel.leic.pc.monitors;


public class Semaphore0 {
    private Object monitor;
    private int permits;

    public Semaphore0(int initialPermits) {
        if (initialPermits >=0)
            permits = initialPermits;
        monitor = new Object();
    }
    public void acquire(long timeout) throws InterruptedException {
        synchronized(monitor) {
            while(permits == 0)
                    monitor.wait();
            permits--;
        }
    }

    public void release() {
        synchronized (monitor) {
            permits++; monitor.notify();
        }
    }
}

