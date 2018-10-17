
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Aula_2018_10_10 {

    /// <summary>
    /// A simple thread pool implementation with a fixed size
    /// </summary>
    class FixedSizeThreadPool {
        private object monitor;
      
        private enum State { Active, InShutdown, Terminated }

        private State state;

        private int aliveThreads;

        LinkedList<Action> requests;

        private Action GetCommand() {
            Action action = null;
            if (requests.Count > 0) {
                action = requests.First.Value;
                requests.RemoveFirst();
            }
            return action;
        }

        private void ExitThread() {
            // check if this is the tread who as the responsability
            // to wakeup shutdown thread 
            if (--aliveThreads == 0) {
                state = State.Terminated;
                Monitor.Pulse(monitor);
            }

        }

        private void Executor() {
            do {
                Action action = null;
                lock(monitor) {
                    do {
                        // if there is a submitted action to 
                        // process go to it!
                        if ((action = GetCommand()) != null)
                            break;
                        // Stop the thread if the shutdown
                        // has started
                        if (state == State.InShutdown) {
                            ExitThread();
                            return;
                        }
                        // maybe a spurious notification, Try again.
                        Monitor.Wait(monitor);
                    }
                    while (true);
                }
                // The action must be executed outside the monitor lock!
                try {
                    action();
                }
                catch (Exception) {  
                }
            }
            while (true);
        }

        public FixedSizeThreadPool(int size) {
            monitor = new object();
            requests = new LinkedList<Action>();
            state = State.Active;
            for(int i=0; i < size; ++i) {
                new Thread(Executor).Start();
            }
            aliveThreads = size;
        }

        /// <summary>
        /// Submit an action to execution on the thread pool
        /// at the lecture we implement this returning a boolean
        /// indicating operation success but we changed here to
        /// launch an exception. 
        /// This seems better since it avois the check in all invocations
        /// </summary>
        /// <param name="action"></param>
        public void Execute(Action action) {
            lock (monitor) {
                if (state != State.Active)
                    throw new InvalidOperationException();
                requests.AddLast(action);
                Monitor.Pulse(monitor);
                
            }
        }

        /// <summary>
        /// iniciate a shutdown operation and wait for his conclusion.
        /// </summary>
        public void Shutdown() {
            lock(monitor) {
                bool interrupted = false;
                if (state != State.Active)
                    throw new InvalidOperationException();
                state = State.InShutdown;

                Monitor.PulseAll(monitor);
                do {
                    try {
                        Monitor.Wait(monitor);
                    }
                    catch(ThreadInterruptedException) {
                        interrupted = true;
                    } 
                }
                while (state != State.Terminated);

                if (interrupted)
                    Thread.CurrentThread.Interrupt();
            }
        }

    }
}
