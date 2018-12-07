using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ThreadUtils;

namespace Aula_2018_11_20 {

    /// <summary>
    /// Shows the problem arise with recursive paralelism
    /// and generic thread pool use:
    /// 
    /// 1- A deadlock scenario  can ourr when worker threads are awaiting 
    ///    for the halves they submited and there are no more threads in the pool 
    ///    to execute this halves - CLR avoid this deadlock injection remaining threads
    ///    but the algorithm efficency is degraded).
    /// 2- Not limiting level paralelism inject too many tasks in the pool
    /// 
    /// Using tasks gives a much better solution avoiding the aforamentioned problems
    /// due to the added work stealing queues.
    /// </summary>
    public class ParQuickSort {
        private const int THRESHOLD = 1000;

        // auxilary variables and methods
        // for collecting and show information about used threads and tasks

        private static int WorkUnits;
        private static void StatsInit() {
            WorkUnits = 0;
            WorkerThreadReport.SetRefTime();
        }
        private static void StatsCollect() {
            WorkerThreadReport.RegisterWorker();
            int usedThreads = WorkerThreadReport.UsedThreads;
            int wi = Interlocked.Increment(ref WorkUnits);

            Console.WriteLine("In work Item {0}, UsedThreads={1}",
                wi, usedThreads);
        }

        private static void StatsShow() {
            Console.WriteLine("Total of work units: {0}", WorkUnits);
            Console.WriteLine("Total pool thread: {0}", WorkerThreadReport.UsedThreads);
            Console.WriteLine();
        }


        // utils
        private static void InitVals(int[] vals) {
            Random r = new Random();

            for (int i = 0; i < vals.Length; ++i) vals[i] = r.Next() % 100;
        }

        private static void PrintVals(int[] vals, int low, int high) {
            Random r = new Random();

            for (int i = low; i <= high; ++i) Console.WriteLine(vals[i]);
        }

        private static bool IsSorted(int[] vals) {
            for (int i = 0; i < vals.Length - 1; ++i) {
                if (vals[i] > vals[i + 1]) return false;
            }
            return true;
        }

        // quick sort versions

        /// <summary>
        /// the partition process in queicksort algorithm
        /// </summary>
        /// <param name="vals"></param>
        /// <param name="low"></param>
        /// <param name="high"></param>
        /// <returns></returns>
        private static int Part(int[] vals, int low, int high) {
            int med = (low + high) / 2;
            int pivot = vals[med];
            int i = low, j = high;
            while (i <= j) {

                while (i <= high && vals[i] < pivot) ++i;
                while (j >= low && vals[j] > pivot) --j;
                if (i <= j) {
                    int tmp = vals[i];
                    vals[i] = vals[j];
                    vals[j] = tmp;
                    ++i;
                    --j;
                }
            }

            return i - 1;
        }

        /// <summary>
        /// Sequential version
        /// </summary>
        /// <param name="vals"></param>
        /// <param name="low"></param>
        /// <param name="high"></param>
        public static void SeqSort(int[] vals, int low, int high) {
            if (low >= high) return;
            int pivot = Part(vals, low, high);

            SeqSort(vals, low, pivot);
            SeqSort(vals, pivot + 1, high);
        }


        /// <summary>
        /// ThreadPool version
        /// </summary>
        /// <param name="vals"></param>
        /// <param name="low"></param>
        /// <param name="high"></param>
        /// <param name="level"></param>
        public static void SortTP(int[] vals, int low, int high, int level) {

            if (high - low < THRESHOLD) { SeqSort(vals, low, high); return; }
            int pivot = Part(vals, low, high);

            if (level > 0) {
                CountdownEvent latch = new CountdownEvent(2);
                ThreadPool.QueueUserWorkItem((s) => {
                    StatsCollect();
                    SortTP(vals, low, pivot, level - 1);
                    latch.Signal();
                }, null);
                ThreadPool.QueueUserWorkItem((s) => {
                    StatsCollect();
                    SortTP(vals, pivot + 1, high, level - 1);
                    latch.Signal();
                }, null);
                latch.Wait();
            }
            else {
                SortTask(vals, low, pivot, 0);
                SortTask(vals, pivot + 1, high, 0);
            }

        }

        /// <summary>
        /// Task version
        /// </summary>
        /// <param name="vals"></param>
        /// <param name="low"></param>
        /// <param name="high"></param>
        /// <param name="level"></param>
        public static void SortTask(int[] vals, int low, int high, int level) {

            if (high - low < THRESHOLD) { SeqSort(vals, low, high); return; }
            int pivot = Part(vals, low, high);

            if (level > 0) {
                Task t1 = Task.Run(() => { StatsCollect(); SortTask(vals, low, pivot, level - 1); });
                Task t2 = Task.Run(() => { StatsCollect(); SortTask(vals, pivot + 1, high, level - 1); });
                Task.WaitAll(t1, t2);

                /* the code above could have been:
                 * 
                 * Parallel.Invoke( 
                 *   () => { StatsCollect(); SortTask(vals, low, pivot, level - 1); },
                 *    () => { StatsCollect(); SortTask(vals, pivot + 1, high, level - 1); }
                 *  );
                 */
            }
            else {
                SortTask(vals, low, pivot, 0);
                SortTask(vals, pivot + 1, high, 0);
            }

        }

        /// <summary>
        /// Sort using thread pool directly
        /// </summary>
        /// <param name="vals"></param>
        /// <param name="level"> Recurrence paralelism level </param>
        public static void SortTP(int[] vals, int level) {
            SortTP(vals, 0, vals.Length - 1, level);
        }

        /// <summary>
        /// Sort using tasks
        /// </summary>
        /// <param name="vals"></param>
        /// <param name="level"> Recurring paralelism level </param>
        public static void SortTask(int[] vals, int level) {
            SortTask(vals, 0, vals.Length - 1, level);
        }


        public static void Test() {

            int[] vals = new int[50000000];
            InitVals(vals);
            int[] vals1 = (int[])vals.Clone();
            int[] vals2 = (int[])vals.Clone();

            StatsInit();
            Stopwatch sw = Stopwatch.StartNew();

            SeqSort(vals, 0, vals.Length - 1);
            sw.Stop();
            Console.WriteLine("sequential sort in {0}ms", sw.ElapsedMilliseconds);
            StatsShow();

            StatsInit();
            sw.Restart();

            // sort using threadpool directly and parallel recurrence level=4
            SortTP(vals1, 2);
            sw.Stop();
            Console.WriteLine("threadpool sort in {0}ms", sw.ElapsedMilliseconds);
            StatsShow();

            StatsInit();
            sw.Restart();
            // sort using tasks and parallel recurrence level=6
            SortTask(vals2, 8);
            sw.Stop();
            Console.WriteLine("task sort in {0}ms", sw.ElapsedMilliseconds);
            StatsShow();

            WorkerThreadReport.ShutdownWorkerThreadReport();
        }
    }
}
