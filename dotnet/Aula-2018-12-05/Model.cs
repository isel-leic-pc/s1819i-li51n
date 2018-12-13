
using AsyncUtils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using System.Threading;
using System.Linq;

namespace ShowImages {
    static class Model {

        /// <summary>
        /// A solution for async image download operation.
        /// Try to describe why is the Unwrap necessary!
        /// 
        /// More alternatives exists on Model1 class
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static Task<Image> DownloadImageFromUrlAsync(String url) {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent",
                  "Mozilla/5.0");
            MemoryStream ms = new MemoryStream();
            return client.GetStreamAsync(url).
                ContinueWith(ant=> {
                    Stream s = ant.Result;
                    return s.CopyToAsync(ms);
                }).
                Unwrap().
                ContinueWith(ant2 => {
                    if (ant2.IsFaulted) throw ant2.Exception;
                    return Image.FromStream(ms);
                });

        }

        /// <summary>
        /// The combinator OrderByCompletion made on 12/12 lecture!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_tasks"></param>
        /// <returns></returns>
        public static IEnumerable<Task<T>> 
            OrderByCompletion<T>(IEnumerable<Task<T>> _tasks) {

            List<Task<T>> tasks = _tasks.ToList();

            var promises = new TaskCompletionSource<T>[tasks.Count];
            var promTaks = new Task<T>[tasks.Count];
           
            for(int i=0; i < promises.Length; ++i) {
                promises[i] = new TaskCompletionSource<T>();
                promTaks[i] = promises[i].Task;
            }

            int index = -1;

            foreach(var t in tasks) {
                t.ContinueWith((ant) => {
                    int idx = Interlocked.Increment(ref index);
                    switch (ant.Status) {
                        case TaskStatus.Faulted:
                            promises[idx].
                                SetException(ant.Exception);
                            break;
                        case TaskStatus.Canceled:
                            promises[idx].
                                SetCanceled();
                            break;
                        case TaskStatus.RanToCompletion:
                            promises[idx].SetResult(ant.Result);
                            break;
                    }
                });
            }
            return promTaks;

        }
       



    }
}
