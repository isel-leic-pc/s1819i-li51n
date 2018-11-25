using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ThreadUtils;
using System.Collections.Concurrent;

namespace Aula_2018_11_21 {
    public class PrimeUtils {
        public static bool IsPrime(long n) {

            //Console.WriteLine("IsPrime thread:  {0}, fromThreadPool={1}",
            //    Thread.CurrentThread.ManagedThreadId,
            //    Thread.CurrentThread.IsThreadPoolThread);
 
            if (n == 2) return true;
            if (n < 2 || n % 2 == 0) return false;
            for (int i = 3; i <= Math.Sqrt(n); i += 2)
                if (n % i == 0) return false;
            return true;
            /*
            Thread.SpinWait(500);
            return true;
            */
            
        }



        public static IEnumerable<long> GetPrimes(long[] vals, int size) {
            LinkedList<long> primes = new LinkedList<long>();
            for (int i = 0; i < size; ++i) {
                if (IsPrime(vals[i]))
                    primes.AddLast(vals[i]);
            }
            return primes;
        }

        public static IEnumerable<long> GetPrimesParTP(long[] vals, int size) {
            LinkedList<long> primes = new LinkedList<long>();
          

            List<Task> tasks = new List<Task>();
            CountdownEvent done = new CountdownEvent(size); 
            for (int i = 0; i < size; ++i) {
                ThreadPool.QueueUserWorkItem((o) => {
                    WorkerThreadReport.RegisterWorker();
                    long val = (long)o;
                    if (IsPrime(val))
                        lock (primes) { primes.AddLast(val); }
                    done.Signal();
                }, vals[i]);
 
            }
            done.Wait();

            return primes;
        }

        public static IEnumerable<long> GetPrimesParTasks(long[] vals, int size) {
            LinkedList<long> primes = new LinkedList<long>();
            //ConcurrentQueue<long> primes = new ConcurrentQueue<long>();
          
           
            List<Task> tasks = new List<Task>();

            for (int i = 0; i < size; ++i) {
                Task t = Task.Factory.StartNew((o) => {
                    WorkerThreadReport.RegisterWorker();
                    long val = (long)o;
                    if (IsPrime(val))
                        lock (primes) { primes.AddLast(val); }
                }, vals[i]);


                tasks.Add(t);
            }
            Task.WaitAll(tasks.ToArray());
            
            return primes;
        }

        public static IEnumerable<long> GetPrimesParFor(long[] vals, int size) {
            //LinkedList<long> primes = new LinkedList<long>();
            List<long> primes = new List<long>();
           
            Parallel.ForEach(vals, (v) => {
                WorkerThreadReport.RegisterWorker();
                if (IsPrime(v))
                    lock (primes) { primes.Add(v); }

            });
            return primes;
        }

        public static IEnumerable<long> GetPrimesParForPartition(long[] vals, int size) {
            //LinkedList<long> primes = new LinkedList<long>();
            List<long> primes = new List<long>();

            Parallel.ForEach(Partitioner.Create(0,size), (v) => {
                WorkerThreadReport.RegisterWorker();
                //Console.WriteLine("Range({0},{1})", v.Item1, v.Item2);
                for (int i=v.Item1; i < v.Item2; ++i)
                    if (IsPrime(vals[i]))
                        lock (primes) { primes.Add(vals[i]); }

            });
            return primes;
        }

        public static IEnumerable<long> GetPrimesParAggregation(long[] vals, int size) {
            LinkedList<List<long>> primes = 
                new LinkedList<List<long>>();

            ParallelLoopResult res = Parallel.For(
                0,
                size,
                () => new List<long>(size/50),
                (i, s, l) => {
                    ParallelLoopState state = s;
                    
                    WorkerThreadReport.RegisterWorker();
                    if (IsPrime(vals[i]))
                        l.Add(vals[i]);
                    return l;
                },
                (l) => {
                    lock (primes) {
                        primes.AddLast(l);
                    }
                });

            var count = primes.Count();
            return  primes.SelectMany(s => s);
   
        }


        public static IEnumerable<long> GetPrimesParPLinq(long[] vals, int size) {
            var primes = vals.AsParallel().
                Take(size).
                Where((v) => {
                    WorkerThreadReport.RegisterWorker();
                    return IsPrime(v);
                });
            primes.Count();
            return primes;
        }


        public static int CountPrimes(long[] vals, int size) {
            int count = 0;
            for (int i = 0; i < size; ++i) {
                if (IsPrime(vals[i]))
                    count++;
            }
            return count;
        }

        public static int CountPrimesParTask(long[] vals, int size) {
            int count = 0;
            List<Task> tasks = new List<Task>();

            for (int i = 0; i < size; ++i) {
                Task t = Task.Factory.StartNew((v) => {
                    long val = (long)v;
                    if (IsPrime(val))
                        Interlocked.Increment(ref count);
                }, vals[i]);
                tasks.Add(t);
            }

            Task.WaitAll(tasks.ToArray());
            return count;
        }

        public static int CountPrimesParFor(long[] vals, int size) {
            int count = 0;
            Parallel.For(0, size, (i) => {
                if (IsPrime(vals[i]))
                    Interlocked.Increment(ref count);
            });
            return count;

        }

        public static int CountPrimesParPLinq(long[] vals, int size) {
            return vals.
                AsParallel().
                Take(size).
                Count( (v) => IsPrime(v));
        }
    }
}
