package isel.leic.pc.utils;

import java.util.concurrent.*;

public class ThreadPoolUtils {

    public static ThreadPoolExecutor createThreadPool(int coreSize, int maxSize, int queueSize, int keepAliveSecs) {
        return  new ThreadPoolExecutor(
                coreSize										/* int corePoolSize */,
                maxSize											/* int maximumPoolSize */,
                keepAliveSecs, TimeUnit.SECONDS			        /* long keepAliveTime, TimeUnit unit */,
                new LinkedBlockingQueue<Runnable>(queueSize)	/* BlockingQueue<Runnable> workQueue */,
                (runnable) -> new Thread(runnable) 				/* ThreadFactory threadFactory */,
                (runnable, executor) -> System.out.println("***runnable rejected") 	/* RejectedExecutionHandler handler */
        );
    }


    public static void shutdownPoolAndWaitTerminationUninterruptibly(ExecutorService exec) {
        exec.shutdown();
        do {
            try {
                exec.awaitTermination(1 * 6, TimeUnit.SECONDS);
                return;
            } catch (InterruptedException ie) {}
        } while (true);
    }
}

