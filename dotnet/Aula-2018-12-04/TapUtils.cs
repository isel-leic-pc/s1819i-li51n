using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Aula_2018_12_04 {
    public static class TapUtils {
        public static Task<int> MyReadAsync(this Stream s, byte[] buf, int ofs, int count) {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

            s.BeginRead(buf, ofs, count, (ar) => {
                try {

                    //Thread.Sleep(3000);
                    Console.WriteLine("Callback in thread {0}",
                      Thread.CurrentThread.ManagedThreadId);
                    int res = s.EndRead(ar);
                    tcs.TrySetResult(res);
                }
                catch (Exception e) {
                    tcs.TrySetException(e);
                }
            }, null);
            return tcs.Task;
        }
    }
}
