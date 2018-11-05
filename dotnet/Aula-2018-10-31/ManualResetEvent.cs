
using SynchUtils;

/// 
/// Jorge Martins, outubro de 2017
/// 
using System;
using System.Threading;


namespace Aula_2018_10_30 {
    /// <summary>
    /// A manual reset event optimized implementation, in the sense that
    /// some parts can be done lockfree, namely:
    ///     1-) On the Wait opertaion, in case the event is signaled
    ///     2-) On the Set operation, when there are no waiters
    ///     
    /// Nothe the memory barriers in order to avoid
    /// reorder of a write-read sequence
    ///     
    /// </summary>
    public class ManualResetEvent {
        private Object monitor = new Object();  // the monitor
        private volatile bool signaled;         // the event state
        private int signalVersion;              // number of signal operations
        private volatile int waiters;           // number of threads possible waiting for event signaling

        public ManualResetEvent(bool initialState) {
            signaled = initialState;
        }

        public bool Wait(int timeout) { // throws InterruptedException
            if (signaled) return true;
            if (timeout == 0) return false;
            lock (monitor) {
                int currentVersion = signalVersion;

                try {
                    waiters++;

                    // necessary to avoid reordering observation between the write
                    // above and the read below (.Net permits this write-read reordering)
                    Thread.MemoryBarrier();

                    if (signaled) return true;



                    // prepare wait
                   
                    TimeoutHolder th = new TimeoutHolder(timeout);
                    do {
                        Monitor.Wait(monitor, th.Value);
                        if (currentVersion != signalVersion)
                            return true;

                        if (th.Timeout)
                            return false;   // abort operation on timeout

                    }
                    while (true);
                }  
                catch(ThreadInterruptedException) {
                    if (currentVersion != signalVersion) {
                        Thread.CurrentThread.Interrupt();
                        return true;
                    }
                    throw;
                }
                finally {
                    waiters--;
                }
            }
        }

        public void Set() {
            if (signaled) return;
            signaled = true;
            // necessary to avoid reordering observation between the write above
            // and the read below (.Net permits this write-read reordering).
            Thread.MemoryBarrier();

            if (waiters > 0) {
                lock (monitor) {
            
                    signalVersion++;
                    if (waiters > 0)
                        Monitor.PulseAll(monitor);
                }
            }
        }

        public void Reset() {
            // since signaled is volatile, the lock is not necessary to guarantee publication
            //lock(monitor) {
            signaled = false;
            //}
        }

    }

}
