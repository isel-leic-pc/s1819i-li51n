#define WHEN_ALL

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Testes {
    class PC16_17i2 {
    }

    public class A { }
    public class B { }

    public class C { }

    public class D { }

    public interface Services {
        A Oper1();
        B Oper2(A a);
        C Oper3(A a);
        D Oper4(B b, C c);
    }

    /// <summary>
    /// Versão assíncrona (TAP) de Services
    /// </summary>
    public interface TapServices {
        Task<A> Oper1Async();
        Task<B> Oper2Async(A a);
        Task<C> Oper3Async(A a);
        Task<D> Oper4Async(B b, C c);
    }

    public class Execute {
        public static D Run(Services svc) {
            var a = svc.Oper1();
            return svc.Oper4(svc.Oper2(a),
                        svc.Oper3(a));
        }

        /// <summary>
        /// Versão assíncrona do método Run
        /// </summary>
        /// <param name="svc"></param>
        /// <returns></returns>
        public async static Task<D> RunAsync(TapServices svc) {
            var a = await svc.Oper1Async();
            var t2 = svc.Oper2Async(a);
            var t3 = svc.Oper3Async(a);

#if WITH_EXCEPTIONS
            B b = null;
            C c = null;

            try {
                b = await t2; 
            }
            catch(Exception e1) {
                try {
                    c = await t3;
                    throw e1;
                }
                catch(Exception e2) {
                    throw new AggregateException(e1, e2);
                }  
            }
            c = await t3;
            return await svc.Oper4Async(b, c);
#elif WHEN_ALL
            await Task.WhenAll(t2,t3);
            return await svc.Oper4Async(t2.Result, t3.Result);
#else
            return await svc.Oper4Async(await t2, await t3);
#endif
        }
    }

    public class MapReduceUtils {
        /// <summary>
        /// Versão original (sequencial)
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <typeparam name="O"></typeparam>
        /// <param name="items"></param>
        /// <param name="selector"></param>
        /// <param name="mapper"></param>
        /// <param name="ctoken"></param>
        /// <returns></returns>
        public static List<O> MapSelectedItems<I, O>(IEnumerable<I> items, Predicate<I> selector,
                                                    Func<I, O> mapper, CancellationToken ctoken) {
            var result = new List<O>();
            foreach (I item in items) {
                ctoken.ThrowIfCancellationRequested();
                if (selector(item)) result.Add(mapper(item));
            }
            return result;
        }

        private static Random r = new Random();

        /// <summary>
        /// Versão paralela
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <typeparam name="O"></typeparam>
        /// <param name="items"></param>
        /// <param name="selector"></param>
        /// <param name="mapper"></param>
        /// <param name="ctoken"></param>
        /// <returns></returns>
        public static List<O> MapSelectedItemsPar<I, O>(IEnumerable<I> items, Predicate<I> selector,
                                                   Func<I, O> mapper, CancellationToken ctoken) {
            ParallelOptions options = new ParallelOptions {
                CancellationToken = ctoken
            };

            var monitor = new object();
            var result = new List<O>();
          
            Parallel.ForEach(items,
                options,
                () => new List<O>(),
                (item, state, l) => {
                    ctoken.ThrowIfCancellationRequested();
                    // para teste
                    Thread.Sleep(r.Next() % 50+50);
                    if (selector(item)) l.Add(mapper(item));
                    return l;
                },
                (l) => {
                    ctoken.ThrowIfCancellationRequested();
                    lock (monitor) {
                        result = result.Concat(l).ToList();
                    }
                });
            result = result.OrderBy((i)=>i).ToList();
            return result;

        }

        public static void TestMapSelectedItemPar() {
            List<int> numbers = Enumerable.Range(1, 100).ToList();
            CancellationTokenSource cts = new CancellationTokenSource();

            try {
                var t = Task.Run(() => {
                    Thread.Sleep(1000);
                    cts.Cancel();
                });
                List<int> result = MapSelectedItemsPar(numbers, (i) => i % 2 == 0, (i) => i * 2, cts.Token);

                result.ForEach((i) => { Console.WriteLine(i); });
            }
            catch(OperationCanceledException e) {
                Console.WriteLine(e.Message);
            }
        }
    }


}
