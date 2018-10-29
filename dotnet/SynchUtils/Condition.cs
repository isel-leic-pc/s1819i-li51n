using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SynchUtils {
    /// <summary>
    /// Extension methods to emulate multiple conditions
    /// on .Net monitors
    /// </summary>
    public static class Condition {

        /// <summary>
        /// non interruptible lock
        /// </summary>
        /// <param name="monitor"></param>
        /// <param name="interrupted"></param>
        public static void EnterUninterruptible(this object monitor, out bool interrupted) {
            interrupted = false;
            while (true) {
                try {
                    Monitor.Enter(monitor);
                    break;

                }
                catch (ThreadInterruptedException) {
                    interrupted = true;
                }
            }  
        }

        /// <summary>
        /// Wait on a condition (any object)
        /// </summary>
        /// <param name="monitor"></param>
        /// <param name="condition"></param>
        /// <param name="timeout"></param>
        public static void Wait(this object monitor, object condition, int timeout) {
            // check if condition and monitor are the same
            if (monitor == condition) {
                Monitor.Wait(monitor, timeout);
                return;
            }
            // if not, first enter condition lock
            Monitor.Enter(condition);
            Monitor.Exit(monitor);
            try {
                Monitor.Wait(condition, timeout);
            }
            finally {
                Monitor.Exit(condition);
                bool interrupted;
                monitor.EnterUninterruptible(out interrupted);
                if (interrupted)
                    throw new ThreadInterruptedException();
            }
        }

        /// <summary>
        /// overload for wait on a condition with no timeout
        /// </summary>
        /// <param name="monitor"></param>
        /// <param name="condition"></param>
        public static void Wait(this object monitor, object condition) {
            monitor.Wait(condition, Timeout.Infinite);
        }

        /// <summary>
        /// condition single notification
        /// </summary>
        /// <param name="monitor"></param>
        /// <param name="condition"></param>
        public static void Notify(this object monitor, object condition) {
            // check if condition and monitor are the same
            if (monitor == condition) {
                Monitor.Pulse(monitor);
                return;
            }
            bool interrupted;
            condition.EnterUninterruptible(out interrupted);
            Monitor.Pulse(condition);
            Monitor.Exit(condition);
            if (interrupted)
                Thread.CurrentThread.Interrupt();
        }

        /// <summary>
        /// condition broadcast notification
        /// </summary>
        /// <param name="monitor"></param>
        /// <param name="condition"></param>
        public static void NotifyAll(this object monitor, object condition) {
            // check if condition and monitor are the same
            if (monitor == condition) {
                Monitor.PulseAll(monitor);
                return;
            }
            bool interrupted;
            condition.EnterUninterruptible(out interrupted);
            Monitor.PulseAll(condition);
            Monitor.Exit(condition);
            if (interrupted)
                Thread.CurrentThread.Interrupt();
        }
    }
}
