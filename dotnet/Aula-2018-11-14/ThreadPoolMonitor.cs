/**
 *
 * ISEL, LEIC, Concurrent Programming
 *
 * Program to monitor worker thread injection in .NET ThreadPool.
 *
 * Carlos Martins, May 2016
 * 
 * Options added by Jorge Martins, Nov 2017
 *
 **/

using System;
using System.Collections.Generic;
using System.Threading;
using ThreadUtils;

class ThreadPoolMonitor {

    static void Main(String[] args) {
        int minWorker, minIocp, maxWorker, maxIocp;
    
        MyOptions o = new MyOptions();
        try {
            o.load(args);
        }
        catch(Exception) {
            o.usage(); Environment.Exit(1);
        }

        Console.WriteLine("Options:");
        Console.WriteLine(o);

        if (o.blocking) {
            Console.WriteLine("\n-- Monitor .NET Thread Pool using a I/O-bound load\n");
        }
        else {
            Console.WriteLine("\n-- Monitor .NET Thread Pool using a CPU-bound load\n");
        }

        WorkerThreadReport.Verbose = true;
        ThreadPool.GetMinThreads(out minWorker, out minIocp);
        ThreadPool.GetMaxThreads(out maxWorker, out maxIocp);

        //ThreadPool.SetMinThreads(2 * Environment.ProcessorCount, minIocp);
        Console.WriteLine("-- processors: {0}; min/max workers: {1}/{2}; min/max iocps: {3}/{4}\n",
                           Environment.ProcessorCount, minWorker, maxWorker, minIocp, maxIocp);

        Console.Write("--Hit <enter> to start, and then <enter> again to terminate...");
        Console.ReadLine();

        for (int i = 0; i < o.nactions; i++) {

            ThreadPool.QueueUserWorkItem((targ) => {
                WorkerThreadReport.RegisterWorker();
                int tid = Thread.CurrentThread.ManagedThreadId;
                Console.WriteLine("-->Action({0}, #{1:00})", targ, tid);
                for (int n = 0; n < o.ntries; n++) {
                    WorkerThreadReport.RegisterWorker();
                    if ( !o.blocking)
					    Thread.SpinWait(o.execTime);		// CPU-bound load
                    else
                        Thread.Sleep(o.blockTime);          // I/O-bound load
                }
                Console.WriteLine("<--Action({0}, #{1:00})", targ, tid);
            }, i);
            Thread.Sleep(o.injectionPeriod);
        }
        int delay = 50;
        do {
            int till = Environment.TickCount + delay;
            do {
                if (Console.KeyAvailable) {
                    goto Exit;
                }
                Thread.Sleep(15);
            } while (Environment.TickCount < till);
            delay += 50;

            //
            // Comment the next statement to allow worker thread retirement!
            //
            /*
			ThreadPool.QueueUserWorkItem(_ => {
				WorkerThreadReport.RegisterWorker();
				Console.WriteLine("ExtraAction() --><-- on worker thread #{0}", Thread.CurrentThread.ManagedThreadId);
			});
			*/

        } while (true);
        Exit:
        Console.WriteLine("-- {0} worker threads were injected", WorkerThreadReport.CreatedThreads);
        WorkerThreadReport.ShowThreads();
        WorkerThreadReport.ShutdownWorkerThreadReport();
    }
}
