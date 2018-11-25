using System;
using System.Threading;

namespace Aula_2018_11_14
{
    /// <summary>
    /// A simple Future implementation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Future<T>
    {
        private const int CREATED = 1;
        private const int RUNNING = 2;
        private const int COMPLETED = 3;
        private const int FAULTED = 1;


        // the value in case of COMPLETED
        private T value;

        // the exception in case of FAULTED
        private Exception exc;
      
        // signaling result done (in COMPLETED or FAULTED) state
        private ManualResetEventSlim  done;

        // current state
        private volatile int state;

        // the value supplier
        private Func<T> supplier;

        public Future(Func<T> supplier) {
            this.supplier = supplier;
            state = CREATED;
            done =  new ManualResetEventSlim(false);
        }

        public void runAsync() {
            // just admite a single run
            if (state != CREATED ||
                Interlocked.CompareExchange(ref state,
                RUNNING, CREATED) != CREATED)
                throw new InvalidOperationException();

            // using thread pool to execute the supplier
            ThreadPool.QueueUserWorkItem((o) => {
                try {
                    value = supplier();
                    state = COMPLETED;
                }
                catch (Exception e) {
                    exc = e;
                    state = FAULTED;
                }
                
                // in any case signal the event
                done.Set();
            });
        }


        public T Result {
            get {
                if (state != COMPLETED && state != FAULTED)
                    done.Wait();
                // Here it's guaranteed that the observation of "exc"
                // and "value" are valid, even if they are not marked as volatiles
                if (exc != null) throw 
                        new Exception("Execution exception", exc);
                return value;
            }
           
        }
    }
}
