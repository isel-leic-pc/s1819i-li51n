package isel.leic.pc.monitors;


import isel.leic.pc.utils.TimeoutHolder;
import org.omg.PortableInterceptor.INACTIVE;

public class Semaphore {
    private Object monitor;
    private int permits;

    public Semaphore(int initialPermits) {
        if (initialPermits >=0)
            permits = initialPermits;
        monitor = new Object();
    }

    public boolean acquire(long timeout) throws InterruptedException{
        synchronized (monitor) {
            // non blocking path
            if (permits > 0) {
                --permits;
                return true;
            }
            if (timeout == 0)
                return false;
            // blocking path
            TimeoutHolder th = new TimeoutHolder(timeout);
            try {
                do {
                    monitor.wait(th.value());
                    if (permits > 0) {
                        --permits;
                        return true;
                    }
                    if (th.value() == 0) {
                        return false;
                    }
                }
                while(true);
            }
            catch(InterruptedException e) {
                if (permits > 0)
                    monitor.notify();
                throw e;
            }
        }
    }

    public void release() {
        synchronized (monitor) {
            ++permits;
            monitor.notify();
        }
    }
}
