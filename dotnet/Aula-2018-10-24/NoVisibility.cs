using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Aula_2018_10_24 {
    class NoVisibility {
        public static  bool  ready = false;
        public static int number = 0;
        public static void Test() {

            //Console.WriteLine("Start!");
            ThreadStart a = () => {
                bool toggle = false;
                while (!ready) toggle = !toggle;
                Console.WriteLine(number);
            };


            Thread t1 = new Thread(a);
            number = 42;
            ready = true;
            t1.Start();
            Thread.Sleep(1000);
            //number = 42;
            //ready = true;
            //Console.WriteLine("Done!");
            t1.Join();
        }
    }
}
