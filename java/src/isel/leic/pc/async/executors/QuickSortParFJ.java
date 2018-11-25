package isel.leic.pc.async.executors;

import isel.leic.pc.utils.WorkerThreadReport;

import java.util.Random;
import java.util.concurrent.ForkJoinPool;
import java.util.concurrent.ForkJoinTask;
import java.util.concurrent.RecursiveAction;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * Just to illustrate ForkJoinPool usage
 * This is a different "beat" from other Java thread pools
 * that is used mainly to support Divide and Conquer parallel algorithms.
 * using the style below
 */
public class QuickSortParFJ {
    private final ForkJoinPool pool;
    private final AtomicInteger workUnits = new AtomicInteger();
    private  void statsInit() {
        WorkerThreadReport.clear();
        workUnits.set(0);
    }
    private  void statsCollect() {
        //int used = WorkerThreadReport.createdThreads();
        int wi = workUnits.incrementAndGet();

        //System.out.printf("used= %d, wi=%d\n", used, wi);
        WorkerThreadReport.registerWorker();
    }

    public  static int part(int[] vals, int low, int high) {
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
    private  void statsShow() {
        System.out.println("Total of work units: " + workUnits);
        System.out.println("Total of worker threads: " + WorkerThreadReport.createdThreads());
        System.out.println();
    }

    private  class QuickSortTask extends RecursiveAction {
        private final int[] array;
        private final int low;
        private final int high;
        private static final int THRESHOLD = 1000;

        /**
         * Creates a {@code QuickSortTask} containing the array and the bounds of the array
         *
         * @param array the array to sort
         * @param low the lower element to start sorting at
         * @param high the non-inclusive high element to sort to
         */
        protected QuickSortTask(int[] array, int low, int high) {
            this.array = array;
            this.low = low;
            this.high = high;
        }


        @Override
        protected void compute() {
            statsCollect();
            if (high - low <= THRESHOLD) {
                seqSort(array, low, high-1);
            } else {
                int pivot = part(array,low,high-1);
                /*
                printVals(array);
                System.out.println("pivot= " + array[pivot]);
                */
                // Execute the sub tasks and wait for them to finish
                invokeAll(new QuickSortTask(array, low, pivot+1), new QuickSortTask(array, pivot+1, high));
            }
        }
    }


    public QuickSortParFJ() {

        pool = ForkJoinPool.commonPool();
    }

    /**
     * Sorts all the elements of the given array using the ForkJoin framework
     * @param array the array to sort
     */
    public void sort(int[] array) {
        statsInit();
        ForkJoinTask<Void> job = pool.submit(new QuickSortTask(array, 0, array.length));
        job.join();
        statsShow();
    }

    private static void seqSort(int[] vals, int low, int high) {
        if (low >= high) return;
        int pivot = part(vals, low, high);

        seqSort(vals, low, pivot);
        seqSort(vals, pivot + 1, high);

    }

    private static void seqSort(int[] vals ) {

        seqSort(vals, 0, vals.length-1);

    }

    private static void  initVals(int[] vals) {
        Random r = new Random();

        for(int i=0; i < vals.length; ++i) vals[i] = r.nextInt() % 100;
    }

    private static void  printVals(int[] vals) {
        Random r = new Random();

        for(int i=0; i < vals.length; ++i) System.out.println(vals[i]);
    }

    private static boolean isSorted(int[] vals) {
        for(int i=0; i < vals.length-1; ++i) {
            if (vals[i] > vals[i+1]) return false;
        }
        return true;
    }


    public static void main(String[] args) {

        int[] vals = new int[50000000];
        initVals(vals);
        int[] vals1 = vals.clone();


        long start = System.currentTimeMillis();
        seqSort(vals);
        long end = System.currentTimeMillis();

        if (!isSorted(vals))
            System.out.println("Sort error in "+ (end-start) + "ms!");
        else
            System.out.println("Sequential sort done in " + (end-start) + "ms!");

        QuickSortParFJ ms = new QuickSortParFJ();
        start = System.currentTimeMillis();
        ms.sort(vals1);
        end = System.currentTimeMillis();
        //printVals(vals);
        if (!isSorted(vals))
            System.out.println("Sort error in "+ (end-start) + "ms!");
        else
            System.out.println("Parallel sort done in " + (end-start) + "ms!");
    }
}
