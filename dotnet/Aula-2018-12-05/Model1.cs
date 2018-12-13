using AsyncUtils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace ShowImages {

    /// <summary>
    /// This class contains exercises and alternative code used in this lecture
    /// </summary>
    static class Model1 {

        /// <summary>
        /// A possible implementation of an asynchronous stream copy
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="src"></param>
        /// <returns></returns>
        public static Task<int> CopyStreamAsync(Stream dst, Stream src) {
            const int MAXBUF = 4096;

            var promise = new TaskCompletionSource<int>();
            byte[] buffer = new byte[MAXBUF];
            int totalBytes = 0;

            // use of C# 7.0 local function instead of declaring a lambda!
            void cont(Task ant1) {
                if (ant1 != null && ant1.IsFaulted) {
                    promise.SetException(ant1.Exception);
                    return;
                }
                src.ReadAsync(buffer, 0, MAXBUF).
                ContinueWith(ant2 => {
                    if (ant2.IsFaulted) {
                        promise.SetException(ant2.Exception);
                        return;
                    }
                    int nr = ant2.Result;
                    if (nr == 0)
                        promise.SetResult(totalBytes);
                    else {
                        totalBytes += nr;
                        dst.WriteAsync(buffer, 0, 4096).ContinueWith(cont);
                    }
                });
            }

            cont(null);
            return promise.Task;
        }

        /// <summary>
        /// An asynchronous iterator example: copying a stream using an asynchronous iterator.
        /// An asynchronous iterator is an iterator that returns an enumeration of tasks. Note 
        /// that, courtesy of yield return and yield break, we can write 
        /// a code very similarly to a synchronous implementation. 
        /// But we must use a runner (async enumerator) of this iterator as showed in the next method.
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        private static IEnumerable<Task> CopyToMemoryStreamInternalAsync(Stream src) {
            const int MAXBUF = 4096;

            byte[] buffer = new byte[MAXBUF];
            MemoryStream ms = new MemoryStream();

            while(true) {
                Task<int> tr;
                yield return tr = src.ReadAsync(buffer, 0, 4096);
                if (tr.Result == 0) {
                    yield return Task.FromResult(ms);
                    yield break;
                }
                ms.Write(buffer, 0, tr.Result);
            }
        }

        /// <summary>
        /// The runner for the previous iterator using the Run method of the AsyncEnumerator class
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static Task<MemoryStream> CopyToMemoryStream2Async(this Stream src) {
            return CopyToMemoryStreamInternalAsync(src).Run<MemoryStream>();
        }

        /// <summary>
        /// An alternative implementation of the DownloadImageFromUrlAsync method
        /// using the "AndThen" extension methods presented in AsyncUtils project
        /// to permit a sequence of continuations that return tasks, without the need of "task Unwrap".
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static Task<Image> DownloadImageFromUrlAsync2(String url) {
            HttpClient client = new HttpClient();
            MemoryStream ms = new MemoryStream();
            return 
                client.GetStreamAsync(url).
                AndThen((s) => {
                    return s.CopyToAsync(ms);
                }).
                AndThen(() => {
                    return Task.FromResult(Image.FromStream(ms));
                });
        }

        /// <summary>
        /// An async iterator for an alternative (just for pedagogycal purposes) implementation
        /// of the asyncnronous image download, using the runner in the next method
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static IEnumerable<Task> DownloadImageFromUrlAsync3Internal(String url) {
            HttpClient client = new HttpClient();
            MemoryStream ms = new MemoryStream();
            
            Task <Stream> ts;
            yield return ts = client.GetStreamAsync(url);

            yield return  ts.Result.CopyToAsync(ms);
             
            yield return Task.FromResult(Image.FromStream(ms));  
        }

        /// <summary>
        /// the runner of the iterator presented in the previous method
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static Task<Image> DownloadImageFromUrlAsync3(String url) {
            return DownloadImageFromUrlAsync3Internal(url).Run<Image>();
        }

        /// <summary>
        /// An alternative using async/await.
        /// To be presented in next lecture (11/12/2018)
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<Image> DownloadImageFromUrlAsync4(String url) {
            HttpClient client = new HttpClient();
          
            Stream s = await client.GetStreamAsync(url);
            Stream ms = new MemoryStream();
            await s.CopyToAsync(ms);
             
            return Image.FromStream(ms); 
        }

    }
}
