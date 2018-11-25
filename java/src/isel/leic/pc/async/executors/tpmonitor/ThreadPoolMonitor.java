package isel.leic.pc.async.executors.tpmonitor;

/**
 *
 * ISEL, LEIC, Concurrent Programming
 *
 * Program to monitor worker thread injection in Java's ThreadPoolExecutor.
 *
 * Carlos Martins, May 2016
 *
 **/



import isel.leic.pc.utils.WorkerThreadReport;
import java.io.IOException;
import java.util.concurrent.ThreadPoolExecutor;
import java.util.concurrent.atomic.AtomicInteger;


import static isel.leic.pc.utils.ThreadPoolUtils.createThreadPool;
import static isel.leic.pc.utils.ThreadPoolUtils.shutdownPoolAndWaitTerminationUninterruptibly;

//
// Monitores the thread pool worker thread injection and retirement.
//

public class ThreadPoolMonitor {

    //
    // Auxiliary methods
    //

    private static int getKey() throws IOException {
        int key = System.in.read();
        do {
            System.in.read();
        } while (System.in.available() != 0);
        return key;
    }

    private static void readln() {
        try {
            do {
                System.in.read();
            } while (System.in.available() != 0);
        } catch (IOException ioex) {}
    }

    private static int availableKeys() {
        do {
            try {
                return System.in.available();
            } catch (IOException ioex) {}
        } while (true);
    }

    private static void sleepUninterruptibly(long milliseconds) {
        long expiresAt = System.currentTimeMillis() + milliseconds;
        do {
            try {
                Thread.sleep(milliseconds);
                break;
            } catch (InterruptedException ie) {}
            milliseconds = expiresAt - System.currentTimeMillis();
        } while (milliseconds > 0);
    }

    private static boolean joinUninterruptibly(Thread toJoin, long millis) {
        do {
            try {
                toJoin.join(millis);
                return !toJoin.isAlive();
            } catch (InterruptedException ie) {}
        } while (true);
    }

    private static final AtomicInteger toSpinOn = new AtomicInteger();

    private static void spinWait(int times) {
        for (int i = 0; i < times; i++) {
            toSpinOn.incrementAndGet();
        }
    }


    /**
     *  If QUEUE_SIZE is greater or equal the number of tasks, only core pool size
     *  worker threads are injected.
     */

    private static ThreadPoolExecutor theThreadPool;
    private static final int KEEP_ALIVE_SECONDS = 200;

    public static void main(String[] args)   {
        MyOptions opt = new MyOptions();

        try {
            opt.load(args);
        }
        catch(Exception e) {
            opt.usage();
            System.exit(1);
        }

        System.out.println(opt);

        WorkerThreadReport.setVerbose(true);
        theThreadPool = createThreadPool(opt.minSize, opt.maxSize, opt.queueSize, KEEP_ALIVE_SECONDS);
        System.out.printf("%n--processors: %d; core pool size: %d; maximum pool size: %d, keep alive time: %d s%n",
                Runtime.getRuntime().availableProcessors(), theThreadPool.getCorePoolSize(),
                theThreadPool.getMaximumPoolSize(), KEEP_ALIVE_SECONDS);

        System.out.print("--hit <enter> to start test and <enter> again to terminate...");
        readln();

        // Allows timeout on core pool threads! Comment for do not allow!
        //theThreadPool.allowCoreThreadTimeOut(true);

        for (int i = 0; i < opt.nactions; i++) {
            final int targ = i;
            theThreadPool.execute(() -> {
                WorkerThreadReport.registerWorker();
                long tid = Thread.currentThread().getId();
                System.out.printf("-->Action(%02d, #%02d)%n", targ, tid);
                for (int n = 0; n < opt.ntries; n++) {
                    WorkerThreadReport.registerWorker();
                    /**
                     * Uncomment one one the following lines in order to select the type of load
                     *
                     * Warning: The thread injection dynamics does not depend on the type of load!
                     */
                    if (opt.blocking)
                        sleepUninterruptibly(opt.blockTime);		// I/O-bound load
                    else
                        spinWait(opt.execTime);					// CPU-bound load

                }
                System.out.printf("<--Action(%02d, #%02d)%n", targ, tid);
            });
            sleepUninterruptibly(opt.injectionPeriod);
        }
        long delay = 50;
        outerLoop:
        do {
            long till = System.currentTimeMillis() + delay;
            do {
                if (availableKeys() > 0) {
                    break outerLoop;
                }
                sleepUninterruptibly(15);
            } while (System.currentTimeMillis() < till);
            delay += 100;

            //
            // Comment the next statement to allow worker thread retirement!
            //
			/*
			theThreadPool.execute(() -> {
				WorkerThreadReport.registerWorker();
				System.out.printf("ExtraAction() --><-- on worker thread #%02d%n", Thread.currentThread().getId());
			});
			*/

        } while (true);

        // Initiate an ordely pool shutdown, and waits until all already submitted tasks to complete
        // The tasks submitted to the pool after it initiates the shutdown are rejected!
        shutdownPoolAndWaitTerminationUninterruptibly(theThreadPool);

        // Show the worker thread usage
        System.out.printf("%n-- %d worker threads were injected%n", WorkerThreadReport.createdThreads());
        WorkerThreadReport.showThreads();

        // Shutdown workwer thread report
        WorkerThreadReport.shutdownWorkerThreadReport();
    }
}