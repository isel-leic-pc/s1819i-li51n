using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aula_2018_12_04 {
    static class AsyncUtils {
        public delegate IAsyncResult starter(byte[] buffer, int ofs, int count,
            AsyncCallback cb, object state);

        public delegate int resolver(IAsyncResult ar);

        public static Task<int> FromAsync(starter s, resolver r, byte[] buffer, 
            int ofs, int count, AsyncCallback cb, object state) {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
            s(buffer, ofs, count, (ar) => {
                try {
                    int res = r(ar);
                    tcs.TrySetResult(res);
                }
                catch (Exception e) {
                    tcs.TrySetException(e);
                }


            }, null);
            return tcs.Task;
        }

        public static Task<int> MyReadAsync2(this Stream s, byte[] buf, int ofs, int count) {
            return FromAsync(s.BeginRead, s.EndRead, buf, ofs, count, null, null);
        }
    }
}
