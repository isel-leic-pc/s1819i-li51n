using System;
 
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using ThreadUtils;
using System.Linq;

namespace Aula_2018_11_21
{
    public class Program
    {
        public static void TestTPL0() {
            Console.WriteLine("In TestTPL0 thread {0}",
                    Thread.CurrentThread.ManagedThreadId);
            Task<int> t = Task.Run(() => {
                Thread.Sleep(2000);
                Console.WriteLine("Hello World from tasks, in thread {0}",
                    Thread.CurrentThread.ManagedThreadId);

                // test throwing an exception
                
                throw new InvalidOperationException("Artificially generated exception in TestTPL0!");
                return 10;
            });

            try {
                Console.WriteLine("result is {0}", t.Result);
            }
            catch(Exception e) {
                Console.WriteLine("An exception of type {0} ocurred: {1}",
                    e.GetType().Name, e.Message);
            }
          
        }

        static void InitVals(long[] vals) {
            Random r = new Random();

            for (int i = 0; i < vals.Length; ++i)
                vals[i] = r.Next();


        }
        public delegate int Counter(long[] vals, int size);

        public static void TestCountPrime(Counter counter, long[] vals, 
                    int size, string msg) {
            const int NRUNS = 10;
            long bestTimeMillis = int.MaxValue;
            int count=0;

            for (int i = 0; i < NRUNS; ++i) {
                Stopwatch sw = Stopwatch.StartNew();

                count = counter(vals, size);
                sw.Stop();
                if (sw.ElapsedMilliseconds < bestTimeMillis)
                    bestTimeMillis = sw.ElapsedMilliseconds;
               
            }
            Console.WriteLine("{0}: there are {1} primes, done in {2}ms!",
                   msg, count, bestTimeMillis);
        }

        public delegate IEnumerable<long> ListAggregation(long[] vals, int size);

        public static void TestList(ListAggregation a, long[] vals, string msg) {
            long bestTimeMillis = int.MaxValue;
            const int NRUNS = 10;
            IEnumerable<long> it=null;
            int threadsUsed = int.MaxValue;

            for (int i=0; i < NRUNS; ++i) {
                WorkerThreadReport.SetRefTime();
                Stopwatch sw = Stopwatch.StartNew();

                it = a(vals, vals.Length);

                sw.Stop();
                if (sw.ElapsedMilliseconds < bestTimeMillis)
                    bestTimeMillis = sw.ElapsedMilliseconds;
                int used = WorkerThreadReport.UsedThreads;
                if (used < threadsUsed) threadsUsed = used;
            }
           

            Console.WriteLine("{0}: total primes is {1}, done in {2} ms!",
                msg, it.Count(), bestTimeMillis);
            Console.WriteLine("{0} threads were used!", threadsUsed);
        }

        public static void TestCountPrimes() {
            long[] vals = new long[200000];
            InitVals(vals);
 
            TestCountPrime(PrimeUtils.CountPrimes, vals, vals.Length, "Sequential");
            TestCountPrime(PrimeUtils.CountPrimesParTask, vals, vals.Length, "Manual tasks");
            TestCountPrime(PrimeUtils.CountPrimesParFor, vals, vals.Length, "Simple parallel for");
            
            TestCountPrime(PrimeUtils.CountPrimesParPLinq, vals, vals.Length, "Parallel Linq");

        }

        public static void TestGetPrimes() {
            long[] vals = new long[200000];
            InitVals(vals);

            TestList(PrimeUtils.GetPrimes, vals,  "Sequential");
            TestList(PrimeUtils.GetPrimesParTP, vals, "Using  threadPool directly");
            TestList(PrimeUtils.GetPrimesParTasks, vals, "Manual tasks");
            TestList(PrimeUtils.GetPrimesParFor, vals, "Simple parallel for");
            TestList(PrimeUtils.GetPrimesParAggregation, vals, "Parallel with aggregation");
            TestList(PrimeUtils.GetPrimesParForPartition, vals, "Parallel with explicit partition");
            TestList(PrimeUtils.GetPrimesParPLinq, vals, "Parallel Linq");
        }

        public static void Main(String[] args) {
            //TestTPL0();

            //TestCountPrimes();

            //Console.WriteLine();
            TestGetPrimes();

            WorkerThreadReport.ShutdownWorkerThreadReport();
        }
    }
}
