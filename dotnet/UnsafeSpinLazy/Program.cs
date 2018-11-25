using System;
using System.Threading;

namespace Exercicios_Parte2 {
    
    /// <summary>
    /// A implementação deste sincronizador, cuja semântica de sincronização é 
    /// idêntica à do tipo Lazy<T>  do . NET Framework, não é threadsafe.Sem utilizar locks,
    /// implemente uma versão threadsafe deste sincronizador.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class UnsafeSpinLazy<T> where T : class {
        private const int UNCREATED = 0, BEING_CREATED = 1, CREATED = 2;
        private int state;
        private Func<T> factory;
        private T value;
        public UnsafeSpinLazy(Func<T> factory) {
            this.factory = factory;
            state = UNCREATED;
        }
        public bool IsValueCreated {
            get {
                return state == CREATED;
            }
        }
        public T Value {
            get {
                SpinWait sw = new SpinWait();
                do {
                    if (state == CREATED) {
                        break;
                    }
                    else if (state == UNCREATED) {
                        state = BEING_CREATED;
                        value = factory();
                        state = CREATED;
                        break;
                    }
                    sw.SpinOnce();
                } while (true);
                return value;
            }
        }
    }
    public class SafeSpinLazy<T> where T : class {
        private const int UNCREATED = 0, BEING_CREATED = 1, CREATED = 2;

        // volatile in orther to have correct publication and retrieving
        private volatile int state;
        private Func<T> factory;
        private T value;

        public SafeSpinLazy(Func<T> factory) {
            state = UNCREATED;
            this.factory = factory;
        }
        public bool IsValueCreated {
            get {
                return state == CREATED;
            }
        }
        public T Value {
            get {
                SpinWait sw = new SpinWait();
                do {
                    int obsState = state;
                    if (obsState == CREATED) {
                        break;
                    }
                    else if (obsState == UNCREATED) {
                        // we must guarantee that the value is craeted just once.
                        // The CAS done that
                        if (Interlocked.CompareExchange(ref state,
                            BEING_CREATED, obsState) == obsState) {
                            value = factory();
                            state = CREATED;
                            break;
                        }
                    }
                    sw.SpinOnce();
                } while (true);
                return value;
            }
        }
    }

    class Program {
        static void Main(string[] args) {
        }
    }
}
