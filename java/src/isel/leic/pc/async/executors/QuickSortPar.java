package isel.leic.pc.async.executors;

import java.util.Random;
import java.util.concurrent.*;
import java.util.concurrent.atomic.AtomicInteger;

import isel.leic.pc.utils.WorkerThreadReport;


/**
 * Show the (not good) behaviour of Java thraed pools on
 * divide and conquer parallel algorithms.
 * The bad consequences are deadlock (FixedThreadPool) or too many threads (CachedThreadPool)
 */
public class QuickSortPar {
    private static final int THRESHOLD = 1000;

    private static AtomicInteger workUnits = new AtomicInteger();

    private static ExecutorService exec;

    // stats

    private static void statsInit() {
        WorkerThreadReport.clear();
        workUnits.set(0);
    }
    private static void statsCollect() {
        int used = WorkerThreadReport.createdThreads();
        int wi = workUnits.incrementAndGet();

        System.out.printf("used= %d, wi=%d\n", used, wi);
        WorkerThreadReport.registerWorker();
    }

    private static void statsShow() {
        System.out.println("Total of work units: " + workUnits);
        System.out.println("Total of worker threads: " + WorkerThreadReport.createdThreads());
        System.out.println();
    }

    private static void seqSort(int[] vals, int low, int high) {
        if (low >= high) return;
        int pivot = part(vals, low, high);

        seqSort(vals, low, pivot);
        seqSort(vals, pivot + 1, high);

    }

    public static int part(int[] vals, int low, int high) {
        int med = (low + high) / 2;
        int pivot = vals[med];
        int i = low, j = high;
        while (i <= j) {

            while (vals[i] < pivot) ++i;
            while ( vals[j] > pivot) --j;
            if (i <= j) {
                int tmp = vals[i];
                vals[i] = vals[j];
                vals[j] = tmp;
                ++i;
                --j;
            }

        }
        return i-1;
    }

    public static void sort(int[] vals, int low, int high, int level) {
        if (high - low < THRESHOLD) { seqSort(vals, low, high); return; }

        int pivot = part(vals, low, high);
        if (level > 0) {
            Future f1 = exec.submit(() -> {
                statsCollect();;
                sort(vals, low, pivot,level-1);
            });
            Future f2 = exec.submit(() -> {
                statsCollect();;
                sort(vals, pivot+1, high,level-1);
            });
            try {
                f1.get();
                f2.get();
            }
            catch(ExecutionException | InterruptedException e) { }
        }
        else {
            sort(vals, low, pivot,0);
            sort(vals, pivot+1, high,0);
        }
    }




    public static void sort(int[] vals, int level) throws ExecutionException, InterruptedException{
        sort(vals, 0, vals.length-1, level);
    }


    static void  initVals(int[] vals) {
        Random r = new Random();

        for(int i=0; i < vals.length; ++i) vals[i] = r.nextInt() % 100;
    }

    static void  printVals(int[] vals) {
        Random r = new Random();

        for(int i=0; i < vals.length; ++i) System.out.println(vals[i]);
    }

    static boolean isSorted(int[] vals) {
        for(int i=0; i < vals.length-1; ++i) {
            if (vals[i] > vals[i+1]) return false;
        }
        return true;
    }

    private static final int FIXED_THREADPOOL_LEVEL=3;
    private static final int CACHED_THREADPOOL_LEVEL=6;

    static void doSort(int[] vals, ExecutorService exec, String msg, int level) {
        try {
            statsInit();
            long start = System.currentTimeMillis();
            if (exec == null) {
                seqSort(vals, 0, vals.length-1);
            }
            else {
                QuickSortPar.exec = exec;

                sort(vals, level);

            }
            long end = System.currentTimeMillis();
            if (!isSorted(vals))
                System.out.println(msg + ": Sort error!");
            else
                System.out.println(msg + ": done in " + (end-start) + "ms!");
            statsShow();
        }
        catch(ExecutionException | InterruptedException e) {

        }
    }

    public static void main(String[] args) {

        int[] vals = new int[50000000];
        initVals(vals);

        int[] vals1 = vals.clone();
        int[] vals2 = vals.clone();


        doSort(vals, null, "sequentialSort", 0);

        ExecutorService exec = null;

        try {
            exec = Executors.newFixedThreadPool(1024);

            doSort(vals1, exec, "fixedThreadPool", FIXED_THREADPOOL_LEVEL);
        }
        finally {
            exec.shutdown();
        }

        try {
            exec = Executors.newCachedThreadPool();
            doSort(vals2, exec, "cachedThreadPool", CACHED_THREADPOOL_LEVEL);
        }
        finally {
            exec.shutdown();
        }

        WorkerThreadReport.shutdownWorkerThreadReport();
    }
}
