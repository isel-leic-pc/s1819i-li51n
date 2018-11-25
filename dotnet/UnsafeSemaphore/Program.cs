using System;
 
using System.Threading;

namespace Exercicios_Parte2 {

    /*
        * Esta implementação reflete a semântica de sincronização de um semáforo, 
        * contudo não é thread-safe.Implemente em Java ou em C#, sem utilizar locks, 
        * uma versão thread-safe deste sincronizador.
        */
    public class UnsafeSemaphore {
        private int maxPermits, permits;
        public UnsafeSemaphore(int initial, int maximum) {
            if (initial < 0 || initial > maximum) throw new InvalidOperationException();
            permits = initial; maxPermits = maximum;
        }
        public bool TryAcquire(int acquires) {
            if (permits < acquires) return false;
            permits -= acquires;
            return true;
        }
        public void Release(int releases) {
            if (permits + releases < permits || permits + releases > maxPermits)
                throw new InvalidOperationException();
            permits += releases;
        }
    }


    // versão thread-safe
    public class SafeSemaphore {
        // marked as readonly, volatile is not necessary in this case
        private readonly int maxPermits;

        // marked as volatile in order to have a correct publication and retrieving
        // (in this case it guarantes that first observation in ecah operation gets the
        // most recent value
        private volatile int permits;

        public SafeSemaphore(int initial, int maximum) {
            if (initial < 0 || initial > maximum)
                throw new InvalidOperationException();
            permits = initial; maxPermits = maximum;
        }

        /// <summary>
        /// This is a standard use of CAS pattern:
        /// 
        /// 1- First, get an observation!
        /// 2- Check if the observed state enables progress. If not, return failure.
        /// 3- Now apply the CAS using the OBSERVED state!
        /// 4- if not CAS successfull, retry from 1
        /// </summary>
        /// <param name="acquires"></param>
        /// <returns></returns>
        public bool TryAcquire(int acquires) {
            int obs;
            do {
                obs = permits;

                if (obs < acquires)
                    // this observation doesn't allow success
                    return false;
            }
            while (Interlocked.CompareExchange(ref permits, obs - acquires, obs) != obs);
            return true;
        }

        /// <summary>
        /// Another standard use of CAS pattern!
        /// </summary>
        /// <param name="acquires"></param>
        /// <returns></returns>
        public void Release(int releases) {
            int obs;
            do {
                obs = permits;
                if (obs + releases < obs || obs + releases > maxPermits)
                    throw new InvalidOperationException();

            }
            while (Interlocked.CompareExchange(ref permits, obs + releases, obs) != obs);

        }
    }
    

    public class Program {
        public static void Main(string[] args) {

        }
    }
    
}
