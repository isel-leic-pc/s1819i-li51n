using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Aula_2018_11_27 {
    class Program {

        public static Task<int> Oper1Async() {
            var task = Task.Run(() => {
                Console.WriteLine("Thread OperAsync1: {0}", Thread.CurrentThread.ManagedThreadId);
                Thread.Sleep(3000);
                return 10;
            });
            return task;
        }

        public static Task<String> Oper2Async(int index) {
            var task = Task.Run(() => {
                Console.WriteLine("Thread OperAsync2: {0}", Thread.CurrentThread.ManagedThreadId);
                Thread.Sleep(3000);
                return "OK!";
            });
            return task;
        }

        
        static void Main(string[] args) {
            
            var t = Oper1Async().ContinueWith((ant) => {
                Console.WriteLine("Thread OperAsync1 continuation: {0}", Thread.CurrentThread.ManagedThreadId);
                int result = ant.Result;
                return Oper2Async(result);
            }, TaskContinuationOptions.ExecuteSynchronously).
            Unwrap().
            ContinueWith((ant2) => {
                Console.WriteLine(ant2.Result);
            }, TaskContinuationOptions.ExecuteSynchronously);
            
           
            Console.Read();
        }
    }
}
