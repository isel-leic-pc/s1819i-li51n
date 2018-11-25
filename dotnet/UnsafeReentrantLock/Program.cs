using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace UnsafeReentrantLock {

    /// <summary>
    /// Esta implementação reflete a semântica do sincronizador reentrant lock 
    /// disponível em C# , contudo não é threadsafe.Usando técnicas de sincronização 
    /// non blocking, implemente, em Java ou em C#, uma versão threadsafe deste 
    /// sincronizador.
    /// </summary>
    class UnsafeSpinReentrantLock {
        private Thread owner;
        private int count;
        public bool tryLock() {
            if (owner == Thread.CurrentThread) { count++; return true; }
            if (owner == null) { owner = Thread.CurrentThread; return true; }
            return false;
        }
        public void Lock() {
            while (!tryLock()) Thread.Yield();
        }
        public void Unlock() {
            if (owner != Thread.CurrentThread)
                throw new InvalidOperationException();
            if (count == 0) owner = null;
            else count--;
        }
    }

    class SafeSpinReentrantLock {
        private volatile Thread owner;
        private int count;

        public bool tryLock() {
            Thread obsOwner;
           
            obsOwner = owner;
            if (obsOwner == Thread.CurrentThread) { count++; return true; }
            
            // here the CAS needed
            return (obsOwner == null &&
               Interlocked.CompareExchange(ref owner, Thread.CurrentThread, null) == null);
        }

        public void Lock() {
            while (!tryLock()) Thread.Yield();
        }

        /// <summary>
        /// Unlock need not to be changed since count 
        /// is used always by the owner thread
        /// </summary>
        public void Unlock() {
            if (owner != Thread.CurrentThread)
                throw new InvalidOperationException();
            if (count == 0) owner = null;
            else count--;
        }
    }


    class Program {
        static void Main(string[] args) {
        }
    }
}
