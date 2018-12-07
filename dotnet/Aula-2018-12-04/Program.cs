using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
namespace Aula_2018_12_04 {
    class Program {
        static void Main(string[] args) {

            byte[] buffer = new byte[1024];

            FileStream fs = new FileStream("../../Program.cs", 
                FileMode.Open,FileAccess.Read,FileShare.Read,4096,true);
            Console.WriteLine("Main in thread {0}",
                       Thread.CurrentThread.ManagedThreadId);


            Task<int> t = fs.MyReadAsync(buffer, 0, 1024).
                ContinueWith(ant => {
                    Console.WriteLine("In thread {0}",
                        Thread.CurrentThread.ManagedThreadId);
                    return ant.Result;
                });

            //Console.WriteLine(t.Result);
            Console.WriteLine(ASCIIEncoding.ASCII.GetString(buffer, 0, 1024));
        }
    }
}
