using System;
using System.Threading;
using System.Diagnostics;

namespace Aula_2018_09_12
{
    class Program
    {
        /// <summary>
        /// Estimate context switch time between two threads
        /// In order to enable counting 1000000 context switch in
        /// each the two test threads are forced.
        /// Both threads have maximum priority and the same processor affinity
        /// in order to force context switches between them.
        /// </summary>
        private static void EstimateCtxSwitchTime()
        {
            int NSWITCH = 1000000;

             Process.GetCurrentProcess().ProcessorAffinity =
                new IntPtr(1);
            ThreadStart func = () =>
            {
                for (int i = 0; i < NSWITCH; ++i)
                    Thread.Yield();
                
            };

            Thread t1 = new Thread(func);
            Thread t2 = new Thread(func);

            t1.Priority = ThreadPriority.Highest;
            t2.Priority = ThreadPriority.Highest;

            Stopwatch cron = Stopwatch.StartNew();
            t1.Start();
            t2.Start();

            // wait for both threads termination
            t1.Join();
            t2.Join();

            cron.Stop();
            long elapsedMillis = cron.ElapsedMilliseconds;
            Console.WriteLine("Estimated ctx switch time = {0} nanos",
                (elapsedMillis * 1000000) / (2 * NSWITCH));

        }

        /// <summary>
        /// Show thread creation
        /// </summary>
        private static void Intro()
        {
            Console.WriteLine("Hello from main thread {0}",
                  Thread.CurrentThread.ManagedThreadId);
            Thread t1 = new Thread(() =>
            {
                Thread.Sleep(5000);

                Console.WriteLine("Hello from thread {0} ",
                    Thread.CurrentThread.ManagedThreadId);
            });
            //Uncomment next line and explain the different behaviour
            //t1.IsBackground = true;
            t1.Start();
        }


        static void Main(string[] args)
        {
            //Intro
            EstimateCtxSwitchTime();
        }
    }
}
