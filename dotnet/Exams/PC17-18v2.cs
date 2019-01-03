using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Testes {

    public class Session { }
    public class Response { }
    public class UserID { }

    public class Request { }
    public interface ITAPServices {
        Task<Session> LoginAsync(UserID uid);
        Task<Response> ExecServiceAsync(Session session, Request request); Task LogoutAsync(Session session);
    }

    public static class Execs {

        /// <summary>
        /// Versão original (promove o bloqueio de threads),
        /// acabando por ser equivalente a um aversão síncrona
        /// </summary>
        /// <param name="svc"></param>
        /// <param name="uid"></param>
        /// <param name="requests"></param>
        /// <returns></returns>
        public static Task<Response[]> ExecServicesOldAsync(ITAPServices svc, UserID uid, Request[] requests) {
            return Task.Run(() => {
                Session session = svc.LoginAsync(uid).Result;
                try {
                    Response[] responses = new Response[requests.Length];
                    for (int i = 0; i < requests.Length; i++)
                        responses[i] = svc.ExecServiceAsync(session, requests[i]).Result;
                    return responses;
                }
                finally { try { svc.LogoutAsync(session).Wait(); } catch { } }
            });
        }

        /// <summary>
        /// Versão que optimiza o paralelismo potencial
        /// </summary>
        /// <param name="svc"></param>
        /// <param name="uid"></param>
        /// <param name="requests"></param>
        /// <returns></returns>
        public static async Task<Response[]> ExecServicesAsync(ITAPServices svc,
                            UserID uid, Request[] requests) {

            Session session = await svc.LoginAsync(uid);
            try {
                Task<Response>[] tasks = new Task<Response>[requests.Length];

                for (int i = 0; i < requests.Length; i++)
                    tasks[i] = svc.ExecServiceAsync(session, requests[i]);

                return await Task.WhenAll(tasks);
            }
            finally { try { await svc.LogoutAsync(session); } catch { } }

        }


    }

    public static class ParallelUtils {
        private static Random r = new Random();
        public static List<O> ParallelMapSelected<I, O>(IEnumerable<I> items,
                Predicate<I> selector, Func<I, O> mapper, CancellationToken ctoken) {
            object monitor = new object();
           
            ParallelOptions options = new ParallelOptions { CancellationToken = ctoken };
            List<O> result = new List<O>();
            Parallel.ForEach(items,
                            options,
                            () => new List<O>(),
                            (i, s, l) => {
                                ctoken.ThrowIfCancellationRequested();
                                   
                                // para teste
                                Thread.Sleep(r.Next() % 50 + 50);
                                if (selector(i)) l.Add(mapper(i));
                                return l;
                            },
                            (l) => {
                                ctoken.ThrowIfCancellationRequested();
                                lock (monitor) {
                                        result.AddRange(l);
                                     
                                }
                            });
            return result;
        }

        public static void TestParallelMapSelected() {
            List<int> numbers = Enumerable.Range(1, 100).ToList();
            CancellationTokenSource cts = new CancellationTokenSource();

            try {
                var t = Task.Run(() => {
                    Thread.Sleep(1000);
                    cts.Cancel();
                });
                List<int> result = ParallelMapSelected(numbers, (i) => i % 2 == 0, (i) => i * 2, cts.Token);

                result.ForEach((i) => { Console.WriteLine(i); });
            }
            catch (OperationCanceledException e) {
                Console.WriteLine(e.Message);
            }
        }
    }



}
