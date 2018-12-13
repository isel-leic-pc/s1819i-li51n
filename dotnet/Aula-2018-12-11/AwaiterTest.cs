using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;

namespace Aula_2018_12_11 {
  
    // A simple class to illustrate use of custom Awaiters in async methods
    class AwaitResult : INotifyCompletion {
        volatile bool res = false;
        public bool IsCompleted {
            get { return res; }
        }
        public bool GetResult() { return false;  }
        public void OnCompleted(Action continuation) {
            //Task.Delay(5000).ContinueWith((ant) => {
                res = true;
                Console.WriteLine("Here we are before continuation, thread {0}",
                Thread.CurrentThread.ManagedThreadId);
                continuation();
                Console.WriteLine("Here we are after continuation, thread {0}",
                    Thread.CurrentThread.ManagedThreadId);
            //});
        }
        internal AwaitResult GetAwaiter() { return this; }
    }



    class AwaiterTest {
        /// <summary>
        /// Illustrates the use of custom Awaiter (AwaitResult)
        /// </summary>
        public static async void BoolProducerAsync() {
            Console.WriteLine("in main thread, thread {0}",
               Thread.CurrentThread.ManagedThreadId);
            var t = new AwaitResult();

            Console.WriteLine("Before await, thread {0}, awaiter is completed? {1}",
              Thread.CurrentThread.ManagedThreadId, t.IsCompleted);
            await t;
            
            Console.WriteLine("in continuation, thread {0}, awaiter is completed? {1}",
                Thread.CurrentThread.ManagedThreadId, t.IsCompleted);
            bool res = t.GetResult();
            Console.WriteLine($"Done with result: {res}");
        }
    }

}
