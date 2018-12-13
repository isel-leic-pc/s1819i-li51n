

//#define SEND_INTERRUPTS

// Comment/Uncomment to select tests
#define AS_LOCK_SYNCH
#define AS_LOCK_ASYNC		
#define ON_PRODUCER_CONSUMER_SYNC	
#define ON_PRODUCER_CONSUMER_ASYNC		

// Uncomment to run the test continously until <enter>
#define RUN_CONTINOUSLY	

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using AsyncUtils;
using System.Diagnostics;


namespace Asynchronizers {

    internal class SemaphoreTests {

        // test semaphore as a mutual exclusion lock using synchronous acquires
        private static bool TestSemaphoreAsLockSync() {

            const int SETUP_TIME = 50;
            const int RUN_TIME = 30 * 1000;
            int THREADS = 50;
            const int MIN_TIMEOUT = 1;
            const int MAX_TIMEOUT = 50;
            const int MIN_CANCEL_INTERVAL = 1;
            const int MAX_CANCEL_INTERVAL = 50;

            Thread[] tthrs = new Thread[THREADS];
            int[] privateCounters = new int[THREADS];
            int[] timeouts = new int[THREADS];
            int[] cancellations = new int[THREADS];
            int issuedInterrupts = 0;
            int[] sensedInterrupts = new int[THREADS];
            int sharedCounter = 0;
            bool exit = false;
            ManualResetEventSlim start = new ManualResetEventSlim();
            SemaphoreSlimPC1 _lock = new SemaphoreSlimPC1(1, 1);

            /**
             * Create and start acquirer/releaser threads
             */

            for (int i = 0; i < THREADS; i++) {
                int tid = i;
                tthrs[i] = new Thread(() => {
                    Random rnd = new Random(Thread.CurrentThread.ManagedThreadId);
                    start.Wait();
                    CancellationTokenSource cts =
                         new CancellationTokenSource(rnd.Next(MIN_CANCEL_INTERVAL, MAX_CANCEL_INTERVAL));
                    do {
                        do {
                            try {

                                if (_lock.Acquire(timeout: rnd.Next(MIN_TIMEOUT, MAX_TIMEOUT), token: cts.Token))
                                    break;
                                timeouts[tid]++;
                            }
                            catch (OperationCanceledException) {
                                cancellations[tid]++;
                                cts.Dispose();
                                cts = new CancellationTokenSource(rnd.Next(MIN_CANCEL_INTERVAL, MAX_CANCEL_INTERVAL));
                            }
                            catch (ThreadInterruptedException) {
                                sensedInterrupts[tid]++;
                            }
                        } while (true);
                        try {
                            Thread.Sleep(0);
                        }
                        catch (ThreadInterruptedException) {
                            sensedInterrupts[tid]++;
                        }
                        sharedCounter++;

                        if (THREADS > 1) {
                            if (rnd.Next(100) < 99) {
                                Thread.Yield();
                            }
                            else {
                                try {
                                    Thread.Sleep(rnd.Next(MIN_TIMEOUT, MAX_TIMEOUT));
                                }
                                catch (ThreadInterruptedException) {
                                    sensedInterrupts[tid]++;
                                }
                            }
                        }

                        // release the lock
                        _lock.Release();
                        privateCounters[tid]++;
                        if (THREADS > 1) {
                            try {
                                if ((privateCounters[tid] % 100) == 0)
                                    Console.Write("[#{0:D2}]", tid);
                            }
                            catch (ThreadInterruptedException) {
                                sensedInterrupts[tid]++;
                            }
                        }
                    } while (!Volatile.Read(ref exit));
                    try {
                        Thread.Sleep(10);
                    }
                    catch (ThreadInterruptedException) {
                        sensedInterrupts[tid]++;
                    }
                });
                tthrs[i].Start();
            }
            Thread.Sleep(SETUP_TIME);
            Stopwatch sw = Stopwatch.StartNew();
            start.Set();
            Random grnd = new Random(Thread.CurrentThread.ManagedThreadId);
            int endTime = Environment.TickCount + RUN_TIME;
            //...
            do {
                Thread.Sleep(grnd.Next(5));

#if SEND_INTERRUPTS
			if (THREADS > 1) {
				tthrs[grnd.Next(THREADS)].Interrupt();
				issuedInterrupts++;
			}
#endif

                if (Console.KeyAvailable) {
                    Console.Read();
                    break;
                }
#if RUN_CONTINOUSLY
		} while (true);
#else
            } while (Environment.TickCount < endTime);
#endif
            Volatile.Write(ref exit, true);
            sw.Stop();
            // Wait until all threads have been terminated.
            for (int i = 0; i < THREADS; i++)
                tthrs[i].Join();

            // Compute results

            Console.WriteLine("\nPrivate counters:");
            int totalAcquisitons = 0, totalInterrupts = 0, totalCancellations = 0;
            for (int i = 0; i < THREADS; i++) {
                totalAcquisitons += privateCounters[i];
                totalInterrupts += sensedInterrupts[i];
                totalCancellations += cancellations[i];
                if (i != 0 && (i % 2) == 0) {
                    Console.WriteLine();
                }
                else if (i != 0) {
                    Console.Write(' ');
                }
                Console.Write("[#{0:D2}: {1}/{2}/{3}/{4}]", i,
                     privateCounters[i], timeouts[i], cancellations[i], sensedInterrupts[i]);
            }
            Console.WriteLine($"\n--shared/private: {sharedCounter}/{totalAcquisitons}");
            Console.WriteLine($"--interrupts issuded/sensed: {issuedInterrupts}/{totalInterrupts}");
            Console.WriteLine($"--cancellations: {totalCancellations}");

            long unitCost = (sw.ElapsedMilliseconds * 1000000L) / sharedCounter;

            Console.Write("--time per acquisition/release: {0} {1}",
                         unitCost >= 1000 ? unitCost / 1000 : unitCost,
                         unitCost >= 1000 ? "us" : "ns");
            return totalAcquisitons == sharedCounter;
        }

        // test semaphore as a mutual exclusion lock using asynchronous acquires

        delegate Task RunAsync(int tid);
        private static bool TestSemaphoreAsLockAsync() {

            const int SETUP_TIME = 50;
            const int RUN_TIME = 60 * 1000;
            const int TASKS = 50;
            const int MIN_TIMEOUT = 1;
            const int MAX_TIMEOUT = 10;

            Task[] tasks = new Task[TASKS];
            int[] privateCounters = new int[TASKS];
            int[] timeouts = new int[TASKS];
            int[] cancellations = new int[TASKS];
            int sharedCounter = 0;
            bool exit = false;
            SemaphoreSlimPC1 _lock = new SemaphoreSlimPC1(1, 1);

            //
            // Create and start acquirer/releaser threads
            //

            Func<int, Task> asyncRun = async (int tid) => {
                Random rnd = new Random(tid);
                do {
                    await Task.Delay(5);
                    do {
                        using (CancellationTokenSource cts = new CancellationTokenSource()) {
                            try {
                                var result = await _lock.AcquireAsync(timeout: rnd.Next(MIN_TIMEOUT, MAX_TIMEOUT), token: cts.Token);
                                if (rnd.Next(100) < 10)
                                    cts.Cancel();
                                if (result)
                                    break;
                                timeouts[tid]++;
                            }
                            catch (AggregateException ae) {
                                ae.Handle((e) => {
                                    if (e is TaskCanceledException) {
                                        cancellations[tid]++;
                                        return true;
                                    }
                                    return false;
                                });
                            }
                            catch (Exception ex) {
                                Console.WriteLine("*** Exception type: {0}", ex.GetType());
                            }
                        }
                    } while (true);
                    sharedCounter++;
                    if (rnd.Next(100) > 95)
                        await Task.Delay(rnd.Next(MIN_TIMEOUT, MAX_TIMEOUT));
                    privateCounters[tid]++;
                    _lock.Release();
                    if (privateCounters[tid] % 100 == 0)
                        Console.Write($"[#{tid:D2}]");
                } while (!Volatile.Read(ref exit));
            };

            // call al lasync methods
            for (int i = 0; i < TASKS; i++) {
                tasks[i] = asyncRun(i);
            }
            Thread.Sleep(SETUP_TIME);
            Stopwatch sw = Stopwatch.StartNew();
            int endTime = Environment.TickCount + RUN_TIME;
            do {
                Thread.Sleep(20);
                if (Console.KeyAvailable) {
                    Console.Read();
                    break;
                }
#if RUN_CONTINOUSLY
		} while (true);
#else
            } while (Environment.TickCount < endTime);
#endif

            Volatile.Write(ref exit, true);
            int sharedSnapshot = Volatile.Read(ref sharedCounter);
            sw.Stop();
            // Wait until all async methods have been terminated.
            Task.WaitAll(tasks);

            // Compute results

            Console.WriteLine("\n\nPrivate counters:");
            int sum = 0;
            for (int i = 0; i < TASKS; i++) {
                sum += privateCounters[i];
                if (i != 0 && i % 3 == 0)
                    Console.WriteLine();
                else if (i != 0)
                    Console.Write(' ');
                Console.Write("[#{0:D2}: {1}/{2}/{3}]", i, privateCounters[i], timeouts[i], cancellations[i]);
            }
            Console.WriteLine();
            long unitCost = (sw.ElapsedMilliseconds * 1000000L) / sharedSnapshot;
            Console.WriteLine("--unit cost of acquire/release: {0} {1}",
                                unitCost > 1000 ? unitCost / 1000 : unitCost,
                                unitCost > 1000 ? "us" : "ns");
            return sum == sharedCounter;
        }


        // Test the semaphore in a producer/consumer context using the synchronous
        // interface	
        private static bool TestSemaphoreInATapProducerConsumerContextSync() {

            const int RUN_TIME = 30 * 1000;
            const int EXIT_TIME = 50;
            const int PRODUCER_THREADS = 10;
            const int CONSUMER_THREADS = 20;
            const int QUEUE_SIZE = PRODUCER_THREADS / 2 + 1;
            const int MIN_TIMEOUT = 1;
            const int MAX_TIMEOUT = 50;
            const int MIN_CANCEL_INTERVAL = 50;
            const int MAX_CANCEL_INTERVAL = 100;
            const int MIN_PAUSE_INTERVAL = 10;
            const int MAX_PAUSE_INTERVAL = 100;
            const int PRODUCTION_ALIVE = 500;
            const int CONSUMER_ALIVE = 10000;

            Thread[] pthrs = new Thread[PRODUCER_THREADS];
            Thread[] cthrs = new Thread[CONSUMER_THREADS];
            int[] productions = new int[PRODUCER_THREADS];
            int[] productionTimeouts = new int[PRODUCER_THREADS];
            int[] productionCancellations = new int[PRODUCER_THREADS];
            int[] consumptions = new int[CONSUMER_THREADS];
            int[] consumptionTimeouts = new int[CONSUMER_THREADS];
            int[] consumptionCancellations = new int[CONSUMER_THREADS];

            bool exit = false;
            BlockingQueueAsync<String> queue = new BlockingQueueAsync<String>(QUEUE_SIZE);

            // Create and start consumer threads.

            for (int i = 0; i < CONSUMER_THREADS; i++) {
                int ctid = i;
                cthrs[i] = new Thread(() => {
                    Random rnd = new Random(ctid);
                    CancellationTokenSource cts = new CancellationTokenSource(rnd.Next(MIN_CANCEL_INTERVAL, MAX_CANCEL_INTERVAL));
                    do {
                        do {
                            try {
                                if (queue.Take(rnd.Next(MIN_TIMEOUT, MAX_TIMEOUT), cts.Token) != null) {
                                    consumptions[ctid]++;
                                    break;
                                }
                                else
                                    consumptionTimeouts[ctid]++;
                            }
                            catch (OperationCanceledException) {
                                consumptionCancellations[ctid]++;
                                cts.Dispose();
                                cts = new CancellationTokenSource(rnd.Next(MIN_CANCEL_INTERVAL, MAX_CANCEL_INTERVAL));
                            }
                            catch (ThreadInterruptedException) {
                                break;
                            }
                            catch (Exception e) {
                                Console.WriteLine($"***Exception: {e.GetType()}: {e.Message}");
                                break;
                            }
                        } while (true);
                        if (consumptions[ctid] % CONSUMER_ALIVE == 0) {
                            Console.Write($"[#c{ctid:D2}]");
                            try {
                                Thread.Sleep(rnd.Next(MIN_PAUSE_INTERVAL, MAX_PAUSE_INTERVAL));
                            }
                            catch (ThreadInterruptedException) {
                                break;
                            }
                        }
                    } while (!Volatile.Read(ref exit));
                });
                cthrs[i].Priority = ThreadPriority.Highest;
                cthrs[i].Start();
            }

            // Create and start producer threads.
            for (int i = 0; i < PRODUCER_THREADS; i++) {
                int ptid = i;
                pthrs[i] = new Thread(() => {
                    Random rnd = new Random(ptid);
                    CancellationTokenSource cts = new CancellationTokenSource(rnd.Next(MIN_CANCEL_INTERVAL, MAX_CANCEL_INTERVAL));
                    do {
                        do {
                            try {
                                if (queue.Put(rnd.Next().ToString(), rnd.Next(MIN_TIMEOUT, MAX_TIMEOUT),
                                              cts.Token)) {
                                    productions[ptid]++;
                                    break;
                                }
                                else
                                    productionTimeouts[ptid]++;
                            }
                            catch (OperationCanceledException) {
                                productionCancellations[ptid]++;
                                cts.Dispose();
                                cts = new CancellationTokenSource(rnd.Next(MIN_CANCEL_INTERVAL, MAX_CANCEL_INTERVAL));
                            }
                            catch (ThreadInterruptedException) {
                                break;
                            }
                            catch (Exception e) {
                                Console.WriteLine($"***Exception: {e.GetType()}: {e.Message}");
                                break;
                            }
                        } while (true);
                        int sleepTime = 0;
                        if (productions[ptid] % PRODUCTION_ALIVE == 0) {
                            Console.Write($"[#p{ptid:D2}]");
                            sleepTime = rnd.Next(MIN_PAUSE_INTERVAL, MAX_PAUSE_INTERVAL);
                        }
                        try {
                            Thread.Sleep(sleepTime);
                        }
                        catch (ThreadInterruptedException) {
                            break;
                        }
                    } while (!Volatile.Read(ref exit));
                });
                pthrs[i].Start();
            }

            // run the test for a while
            int endTime = Environment.TickCount + RUN_TIME;
            do {
                Thread.Sleep(50);
                if (Console.KeyAvailable) {
                    Console.Read();

                    break;
                }
#if RUN_CONTINOUSLY
		} while (true);
#else
            } while (Environment.TickCount < endTime);
#endif
            Volatile.Write(ref exit, true);
            Thread.Sleep(EXIT_TIME);

            // Wait until all producer have been terminated.
            int sumProductions = 0;
            for (int i = 0; i < PRODUCER_THREADS; i++) {
                if (pthrs[i].IsAlive)
                    pthrs[i].Interrupt();
                pthrs[i].Join();
                sumProductions += productions[i];
            }

            int sumConsumptions = 0;
            // Wait until all consumer have been terminated.
            /*
            for (int i = 0; i < CONSUMER_THREADS; i++) {
                if (cthrs[i].IsAlive) {
                    //cthrs[i].Interrupt();
                    cthrs[i].Join();
                    break;
                }
               
                sumConsumptions += consumptions[i];
            }
            */
            // Display consumer results
            Console.WriteLine("\nConsumer counters:");
            for (int i = 0; i < CONSUMER_THREADS; i++) {
                if (i != 0 && i % 2 == 0) {
                    Console.WriteLine();
                }
                else if (i != 0) {
                    Console.Write(' ');
                }
                Console.Write("[#c{0:D2}: {1}/{2}/{3}]", i, consumptions[i], consumptionTimeouts[i],
                                consumptionCancellations[i]);
            }

            // consider not consumed productions
            sumConsumptions += queue.Count;

            Console.WriteLine("\nProducer counters:");
            for (int i = 0; i < PRODUCER_THREADS; i++) {
                if (i != 0 && i % 2 == 0) {
                    Console.WriteLine();
                }
                else if (i != 0) {
                    Console.Write(' ');
                }
                Console.Write("[#p{0:D2}: {1}/{2}/{3}]", i, productions[i], productionTimeouts[i],
                               productionCancellations[i]);
            }
            Console.WriteLine("\n--productions: {0}, consumptions: {1}", sumProductions, sumConsumptions);
            return sumConsumptions == sumProductions;
        }

        // Test the semaphore in a producer/consumer context using asynchronous TAP acquires	
        private static bool TestSemaphoreInATapProducerConsumerContextAsync() {

            const int RUN_TIME = 30 * 1000;
            const int EXIT_TIME = 50;
            const int PRODUCER_THREADS = 1; // 10;
            const int CONSUMER_THREADS = 1; // 20;
            const int QUEUE_SIZE = PRODUCER_THREADS / 2 + 1;
            const int MIN_TIMEOUT = 1;
            const int MAX_TIMEOUT = 50;
            const int MIN_CANCEL_INTERVAL = 50;
            const int MAX_CANCEL_INTERVAL = 100;
            const int MIN_PAUSE_INTERVAL = 10;
            const int MAX_PAUSE_INTERVAL = 100;
            const int PRODUCTION_ALIVE = 500;
            const int CONSUMER_ALIVE = 10000;

            Thread[] pthrs = new Thread[PRODUCER_THREADS];
            Thread[] cthrs = new Thread[CONSUMER_THREADS];
            int[] productions = new int[PRODUCER_THREADS];
            int[] productionTimeouts = new int[PRODUCER_THREADS];
            int[] productionCancellations = new int[PRODUCER_THREADS];
            int[] consumptions = new int[CONSUMER_THREADS];
            int[] consumptionTimeouts = new int[CONSUMER_THREADS];
            int[] consumptionCancellations = new int[CONSUMER_THREADS];

            bool exit = false;
            BlockingQueueAsync<String> queue = new BlockingQueueAsync<String>(QUEUE_SIZE);

            // Create and start consumer threads.

            for (int i = 0; i < CONSUMER_THREADS; i++) {
                int ctid = i;
                cthrs[i] = new Thread(() => {
                    Random rnd = new Random(ctid);
                    CancellationTokenSource cts = new CancellationTokenSource(rnd.Next(MIN_CANCEL_INTERVAL, MAX_CANCEL_INTERVAL));
                    do {
                        try {
                            if (queue.TakeAsync(rnd.Next(MIN_TIMEOUT, MAX_TIMEOUT), cts.Token).Result != null)
                                consumptions[ctid]++;
                            else
                                consumptionTimeouts[ctid]++;
                        }
                        catch (AggregateException ae) {
                            if (ae.InnerException is TaskCanceledException) {
                                consumptionCancellations[ctid]++;
                                cts.Dispose();
                                cts = new CancellationTokenSource(rnd.Next(MIN_CANCEL_INTERVAL, MAX_CANCEL_INTERVAL));
                            }
                            else {
                                Console.WriteLine($"***Exception: {ae.InnerException.GetType()}: {ae.InnerException.Message}");
                                break;
                            }
                        }
                        catch (ThreadInterruptedException) {
                            break;
                        }
                        if (consumptions[ctid] % CONSUMER_ALIVE == 0) {
                            Console.Write($"[#c{ctid:D2}]");
                            try {
                                Thread.Sleep(rnd.Next(MIN_PAUSE_INTERVAL, MAX_PAUSE_INTERVAL));
                            }
                            catch (ThreadInterruptedException) {
                                break;
                            }
                        }
                    } while (!Volatile.Read(ref exit));
                });
                cthrs[i].Priority = ThreadPriority.Highest;
                cthrs[i].Start();
            }

            // Create and start producer threads.
            for (int i = 0; i < PRODUCER_THREADS; i++) {
                int ptid = i;
                pthrs[i] = new Thread(() => {
                    Random rnd = new Random(ptid);
                    CancellationTokenSource cts = new CancellationTokenSource(rnd.Next(MIN_CANCEL_INTERVAL, MAX_CANCEL_INTERVAL));
                    do {
                        do {
                            try {
                                var putTask = queue.PutAsync(rnd.Next().ToString(), rnd.Next(MIN_TIMEOUT, MAX_TIMEOUT), cts.Token);
                                if (putTask.Result) {
                                    productions[ptid]++;
                                    break;
                                }
                                else
                                    productionTimeouts[ptid]++;
                            }
                            catch (AggregateException ae) {
                                if (ae.InnerException is TaskCanceledException) {
                                    productionCancellations[ptid]++;
                                    cts.Dispose();
                                    cts = new CancellationTokenSource(rnd.Next(MIN_CANCEL_INTERVAL, MAX_CANCEL_INTERVAL));
                                }
                                else {
                                    Console.WriteLine($"***Exception: {ae.InnerException.GetType()}: { ae.InnerException.Message}");
                                    break;
                                }
                            }
                            catch (ThreadInterruptedException) {
                                break;
                            }
                        } while (true);
                        if (productions[ptid] % PRODUCTION_ALIVE == 0) {
                            Console.Write($"[#p{ptid:D2}]");
                            try {
                                Thread.Sleep(rnd.Next(MIN_PAUSE_INTERVAL, MAX_PAUSE_INTERVAL));
                            }
                            catch (ThreadInterruptedException) {
                                break;
                            }
                        }
                    } while (!Volatile.Read(ref exit));
                });
                pthrs[i].Start();
            }

            // run the test for a while
            int endTime = Environment.TickCount + RUN_TIME;
            do {
                Thread.Sleep(50);
                if (Console.KeyAvailable) {
                    Console.Read();
                    break;
                }
#if RUN_CONTINOUSLY
        } while (true);
#else
            } while (Environment.TickCount < endTime);
#endif
            Volatile.Write(ref exit, true);
            Thread.Sleep(EXIT_TIME);

            // Wait until all producer have been terminated.
            int sumProductions = 0;
            for (int i = 0; i < PRODUCER_THREADS; i++) {
                if (pthrs[i].IsAlive)
                    pthrs[i].Interrupt();
                pthrs[i].Join();
                sumProductions += productions[i];
            }

            int sumConsumptions = 0;
            // Wait until all consumer have been terminated.
            for (int i = 0; i < CONSUMER_THREADS; i++) {
                if (cthrs[i].IsAlive)
                    cthrs[i].Interrupt();
                cthrs[i].Join();
                sumConsumptions += consumptions[i];
            }

            // Display consumer results
            Console.WriteLine("\nConsumer counters:");
            for (int i = 0; i < CONSUMER_THREADS; i++) {
                if (i != 0 && i % 2 == 0)
                    Console.WriteLine();
                else if (i != 0)
                    Console.Write(' ');
                Console.Write($"[#c{i:D2}: {consumptions[i]}/{consumptionTimeouts[i]}/{consumptionCancellations[i]}]");
            }

            // consider not consumed productions
            sumConsumptions += queue.Count;

            Console.WriteLine("\nProducer counters:");
            for (int i = 0; i < PRODUCER_THREADS; i++) {
                if (i != 0 && i % 2 == 0)
                    Console.WriteLine();
                else if (i != 0)
                    Console.Write(' ');
                Console.Write($"[#p{i:D2}: {productions[i]}/{productionTimeouts[i]}/{productionCancellations[i]}]");
            }
            Console.WriteLine($"\n--productions: {sumProductions}, consumptions: {sumConsumptions}");
            return sumConsumptions == sumProductions;
        }

        static void Main() {

#if AS_LOCK_SYNCH
		Console.WriteLine("\n-->test semaphore as lock using synchronous acquires: {0}",
							  TestSemaphoreAsLockSync() ? "passed" : "failed");
#endif

#if AS_LOCK_ASYNC
		
		Console.WriteLine("\n-->test semaphore as lock using asynchronous acquires: {0}",
							  TestSemaphoreAsLockAsync() ? "passed" : "failed");
#endif

#if ON_PRODUCER_CONSUMER_SYNC
		
		Console.WriteLine("\n-->test semaphore in a synchronous producer/consumer context: {0}",
						  TestSemaphoreInATapProducerConsumerContextSync() ? "passed" : "failed");
#endif

#if ON_PRODUCER_CONSUMER_ASYNC


            Console.WriteLine("\n-->test semaphore in a asynchronous producer/consumer context: {0}",
						  TestSemaphoreInATapProducerConsumerContextAsync() ? "passed" : "failed");
#endif
        }
    }
}

