using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Testes {
    public class PG17_18i1 {

        public R Map<T,R>(T t) { return default(R);  }
        public R Reduce<R>(R r1, R r2) { return default(R); }

        /// <summary>
        /// Versão sequencial(síncrona)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="elems"></param>
        /// <param name="initial"></param>
        /// <returns></returns>
        public R MapReduce<T,R>(T[] elems, R initial) {
            for  (int i = 0 ; i < elems.Length; ++i) 
		      initial = Reduce(Map<T,R>(elems[i]), initial);
            return initial;
        }

        /// Variantes assíncronas de Map e Reduce
        /// 
        public Task<R> MapAsync<T, R>(T t) { return null; }
        public Task<R> ReduceAsync<R>(R r1, R r2) { return null; }

        /// <summary>
        /// Versão assíncrona sem preocupação em tratar eventuais execepções
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="elems"></param>
        /// <param name="initial"></param>
        /// <returns></returns>
        public async Task<R> MapReduceAsync<T, R>(T[] elems, R initial) {
            List<Task<R>> tres = new List<Task<R>>();

            for (int i = 0; i < elems.Length; ++i)
                tres.Add(MapAsync<T, R>(elems[i]));
            while(tres.Count > 0) {
                Task<R> t = await Task.WhenAny(tres);
                initial = await ReduceAsync(t.Result, initial);
                tres.Remove(t);
            }
            return initial;
        }

    }
}
