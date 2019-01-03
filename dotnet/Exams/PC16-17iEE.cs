using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Testes {
    public class PC16_17iEE {
        public class Data { }

        public class Result {
            public static Result SATURATED = new Result();
        }

        public static Result Reduce(Result r, Result d) {
            return null;
        }

        public static Result Map( Data d) {
            return null;
        }

        /// <summary>
        /// versão original (sequencial)
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Result MapReduceSeq(IEnumerable<Data> data) {
            Result r = null;
            foreach (var datum in data)
                if ((r = Reduce(r, Map(datum))) == Result.SATURATED) break;
            return r;
        }

        /// <summary>
        /// Versão paralela
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Result MapReducePar(IEnumerable<Data> data) {
            Result r = null;
            object monitor = new object();
            Parallel.ForEach(data,
                            () =>  (Result) null,
                            (d, s, l) => {
                                Result lr = Reduce(l, Map(d));
                                if (lr == Result.SATURATED) s.Stop();
                                return lr;
                            },
                            (res) => {
                                Result r1 = Volatile.Read(ref r);
                                if (r1 == Result.SATURATED) return;
                                lock (monitor)  
                                    if (r != Result.SATURATED)
                                     r = Reduce(r, res);    
                                }
                            );
            return r;
        }

    }
}
