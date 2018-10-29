
using SynchUtils;
/// Implementação (incorreta) do sincronizador ManualResetEvent 
/// com fast path lock-free, isto é , o teste preliminar na operação de wait
/// é feito fora do monitor, bem como o teste preliminar na operração set
/// 
/// Jorge Martins, outubro de 2017
/// 
using System;
using System.Threading;


namespace Aula_2018_10_16 {
    /// <summary>
    /// A manual reset event implementation bases on the generation(version) of notification.
    /// (Equivalent to batch notification).
    /// </summary>
    public class ManualResetEvent {
        private Object monitor = new Object();  // the monitor
        private bool signaled;         // the event state
        private int signalVersion;    // number of signal operations

        public ManualResetEvent(bool initialState) {
            signaled = initialState;
        }

        public bool await(int timeout) { // throws InterruptedException
            lock(monitor) {

                if (signaled) return true;
                if (timeout == 0) return false;
                // prepare wait
                int currentVersion = signalVersion;

                try {
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
            }
        }

        public void set() {
            lock(monitor) { 
                if (!signaled) {
                    signaled = true;
                    signalVersion++;
                    Monitor.PulseAll(monitor);
                }    
            }
        }

        public void reset() {
            lock(monitor) {
                signaled = false;
            }
        }

       

    }

}
