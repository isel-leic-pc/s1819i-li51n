using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Testes {
    public class Question4 {

        // Query

        public interface IServices {
            Uri[] GetVoters(String question);
            bool GetAnswer(Uri voter, String question);
        }

        public interface IServicesAsync {
            Task<Uri[]> GetVotersAsync(String question);
            Task<bool> GetAnswerAsync(Uri voter, String question, CancellationToken token);
        }

        public static bool Query(IServices svc, String question) {
            Uri[] voters = svc.GetVoters(question);
            int agree, n;
            for (agree = 0, n = 1; n <= voters.Length; n++) {
                agree += svc.GetAnswer(voters[n - 1], question) ? 1 : 0;
                if (agree > (voters.Length / 2) || n - agree > (voters.Length / 2))
                    break;
            }
            return agree > voters.Length / 2;
        }

        public static async Task<bool> QueryAsync(IServicesAsync svc, String question) {
            Uri[] voters = await svc.GetVotersAsync(question);
            int agree = 0;
            List<Task<bool>> votes = new List<Task<bool>>();
            CancellationTokenSource cts = new CancellationTokenSource();

            // to optimize potencial paralelism launch all operations
            for (int n = 1; n <= voters.Length; n++)
                votes.Add(svc.GetAnswerAsync(voters[n], question, cts.Token));

            // now process the results as while they are coming
            for (int n = 1; n <= voters.Length; n++) {
                var t = await Task.WhenAny(votes);
                agree += t.Result ? 1 : 0;
                votes.Remove(t);
                if (agree > (voters.Length / 2) || n - agree > (voters.Length / 2))
                    break;
            }

            // Cancel pending GetAnswer operations
            cts.Cancel();

            // Wait for completion, just for guaranteed observation of all tasks
            await Task.WhenAll(votes);

            return agree > voters.Length / 2;
        }

        // Select

        /// <summary>
        /// Versão original
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="keys"></param>
        /// <param name="count"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        static List<T> Select<T>(IEnumerable<T> items, IEnumerable<T> keys, int count, CancellationToken ct) {
            List<T> result = new List<T>();
            foreach (T item in items) {
                ct.ThrowIfCancellationRequested();
                foreach (T key in keys)
                    if (item.Equals(key)) { result.Add(item); break; }
                if (result.Count >= count)
                    break;
            }
            return result;
        }

        static List<T> SelectPar<T>(IEnumerable<T> items, IEnumerable<T> keys, int count, CancellationToken ct) {
            object monitor = new object();
            List<T> result = new List<T>();
            bool stop = false;

            Parallel.ForEach(items,
                () => new List<T>(),
                (i, s, lr) => {
                    ct.ThrowIfCancellationRequested();
                    foreach (T key in keys)
                        if (i.Equals(key)) { lr.Add(i); break; }
                    if (result.Count >= count) {
                        // to permit premature termination of global aggregation
                        Volatile.Write(ref stop, true);
                        s.Break();
                    }

                    return lr;
                },
                (lr) => {
                    if (Volatile.Read(ref stop)) return;
                    lock (monitor) {
                        result = result.Concat(lr).ToList();
                    }
                });
            
            return result;
        }

    }

}
