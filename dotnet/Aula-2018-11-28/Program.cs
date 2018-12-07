using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
namespace Aula_2018_11_28 {
    class Program {

        private static void CancellationTest() {
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;
            cts.Cancel();
            Task t = Task.Run(() => {
                for(int i=0; i < 10; ++i) {
                    //ct.ThrowIfCancellationRequested();
                    Thread.Sleep(1000);
                }
            }, cts.Token);
            Thread.Sleep(1000);
      

            try {
                t.Wait();
            }
            catch (AggregateException e) {
                Console.WriteLine("Exception ocurred: {0}", e.Message);
                e.Handle((e1) => {
                    Console.WriteLine("Inner exception {0} of type {1}",
                        e1.Message,
                        e1.GetType().Name);
                    return true;
                });
            }

            Console.WriteLine("Task terminated in state: {0}", t.Status);
        }

        private static Task<String> runAsync1() { 
            var t = TaskUtils.PCDelay(2000);

            var t2 = t.ContinueWith((ant) => {
                Console.WriteLine("end of task 1");
               
                return "Task 1";
            });
            return t2;
        }

        private static Task<String> runAsync2() {
            var tcs = new TaskCompletionSource<String>();
            var t = TaskUtils.PCDelay(2000);

            var t2 = t.ContinueWith((ant) => {
                Console.WriteLine("end of task 2");
                tcs.SetResult("Task 2");
            });
            return tcs.Task;
        }


        public static void TestWaitAll() {
            Task<String> t1 = runAsync1();


            var t2 = runAsync2();

            var tall = TaskUtils.PCWhenAll(t1, t2);

            tall.ContinueWith((ant) => {
                Console.WriteLine("Task antecedent in state: {0}", ant.Status);
                Console.WriteLine("All tasks are done");
            });
        }
        static void Main(string[] args) {
            TestWaitAll();
            //CancellationTest();
            Console.ReadLine();
        }
    }
}
