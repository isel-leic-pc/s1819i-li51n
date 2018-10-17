using SynchUtils;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Aula_2018_10_16 {
  
    public class ManualResetEvent {
        private bool signaled;

        private object monitor;

        private int notifyVersion;

        public ManualResetEvent(bool initialState) {
            signaled = initialState;
            monitor = new object();
            notifyVersion = 0;
        }

        public void Set() {
            lock (monitor) {
                if (!signaled) {
                    signaled = true;
                    notifyVersion++;
                    Monitor.PulseAll(monitor);
                }
              
            }
        }

        public void Reset() {
            lock (monitor) {
                signaled = false;
            }
        }

        public bool Await(int timeout) {
            lock (monitor) {
                if (signaled) return true;
                if (timeout == 0) return false;

                TimeoutHolder th = new TimeoutHolder(timeout);
                int currVersion = notifyVersion;
                try {

                    do  {
                        Monitor.Wait(monitor, th.Value);
                        if (currVersion != notifyVersion) return true;
                        if (th.Timeout) return false;
                    }
                    while (true) ;
                }
                catch (ThreadInterruptedException) {
                    if (signaled) {
                        Thread.CurrentThread.Interrupt();
                        return true;
                    }
                    throw;
                }
            }
        }

    }

}
