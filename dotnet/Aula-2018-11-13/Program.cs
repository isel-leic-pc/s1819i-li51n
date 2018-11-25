using System;
using System.Threading;

namespace Aula_2018_11_13 {
    class Program {
        public class AsyncInvokeTest {

            static bool IsPrime(int n) {

                Console.WriteLine("IsPrime thread:  {0}, fromThreadPool={1}",
                    Thread.CurrentThread.ManagedThreadId,
                    Thread.CurrentThread.IsThreadPoolThread);
                if (n == 2) return true;
                if (n < 2 || n % 2 == 0) return false;
                for (int i = 3; i <= Math.Sqrt(n); i += 2)
                    if (n % i == 0) return false;
                return true;


            }
 
            /// <summary>
            /// Just ilustrate the APM style for asynchronous invocation
            /// using delegate's BeginInvoke and EndInvoke
            /// </summary>
            /// <param name="i"></param>
            public static void CalculateIsPrime1(int i) {
                Console.WriteLine("Main thread:  {0}, fromThreadPool={1}",
                            Thread.CurrentThread.ManagedThreadId,
                            Thread.CurrentThread.IsThreadPoolThread);
                Func<int, bool> pFunc = IsPrime;

                IAsyncResult ar = pFunc.BeginInvoke(i, null, null);

                // Now do other things...

                bool res = pFunc.EndInvoke(ar);
                Console.WriteLine("The number is prime? {0}", res);
            }

            /// <summary>
            /// Just ilustrate the APM style for asynchronous invocation
            /// using delegate's BeginInvoke specifying a callback
            /// </summary>
            /// <param name="i"></param>
            public static void CalculateIsPrime2(int i) {
                Console.WriteLine("Main thread:  {0}, fromThreadPool={1}",
                            Thread.CurrentThread.ManagedThreadId,
                            Thread.CurrentThread.IsThreadPoolThread);
                Func<int, bool> pFunc = IsPrime;
                ManualResetEventSlim done = new ManualResetEventSlim(false);
                pFunc.BeginInvoke(i,
                    (ar) => {

                        Console.WriteLine("The number is prime? {0}",
                            pFunc.EndInvoke(ar));
                        done.Set();
                    }, null);


                done.Wait();
            }
        }
        
        static void Main(string[] args) {
            try {
                Console.WriteLine("BeginInvoke and EndInvoke without callback:");
                AsyncInvokeTest.CalculateIsPrime1(23);

                Console.WriteLine();
                Console.WriteLine("BeginInvoke and EndInvoke with callback:");
                AsyncInvokeTest.CalculateIsPrime2(23);
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
         
    }
}
