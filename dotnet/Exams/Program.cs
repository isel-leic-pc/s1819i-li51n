using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Testes {

   
    class Program {
        public static void TestMapSelectedItemPar() {
            for (int i = 0; i < 10; ++i) MapReduceUtils.TestMapSelectedItemPar();
        }

        public static void TestParallelMapSelected() {
            for (int i = 0; i < 10; ++i) ParallelUtils.TestParallelMapSelected();
        }

        static void Main(string[] args) {
            TestParallelMapSelected();
        }
    }
}
