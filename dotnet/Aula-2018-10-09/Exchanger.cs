using SynchUtils;
using System.Threading;

/// <summary>
/// An Exchanger implementation
/// Note that in the series the KeyedExchanger could be viewed somehow as a Dictionary of Exchangers,
/// with an int key
/// </summary>
namespace Aula_2018_10_09 {
    public class Exchanger<T> {
        private readonly object monitor;

        private class PairRequest {
            internal T msg;
            internal PairRequest( T msg) { this.msg = msg;  }
        }

        // we could have used a list of PairRequests, but due to the semantic of the synchronizer
        // there will at most a single entry in the list!
        PairRequest current;

        public Exchanger() {
            monitor = new object();
            current = null; // no pairing at start
        }

        public bool Exchange(T mine, int timeout, out T yours) {
            yours = default(T); // just to satisfy the compiler
            lock(monitor) {
                if (current != null) { // a partner is waiting...
                    // process the pairing using execution delegation technique
                    yours = current.msg;
                    current.msg = mine;
                    Monitor.Pulse(monitor);
                    current = null; // the pairing is done, so we must "hide" it 
                    return true;
                }
                // prepare waiting
                PairRequest myPair = current = new PairRequest(mine);
                TimeoutHolder th = new TimeoutHolder(timeout);
                try {
                    do {
                        Monitor.Wait(monitor, th.Value);
                        // check success
                        if ( !object.ReferenceEquals(myPair.msg, mine)) { // the pairing is done
                            yours = myPair.msg;
                            return true;
                        }
                        // check timeout
                        if (th.Timeout) {
                            // abort current pairing
                            current = null;
                            return false;
                        }
                    } while (true);
                }
                catch(ThreadInterruptedException) {
                    if (!object.ReferenceEquals(myPair.msg, mine)) { // the pairing is done
                        yours = myPair.msg;
                        Thread.CurrentThread.Interrupt();
                        return true;
                    }
                    // abort current pairing
                    current = null;
                    throw;
                }
            }
        }
    }
}
