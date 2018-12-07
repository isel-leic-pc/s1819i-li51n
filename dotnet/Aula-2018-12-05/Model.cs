
using AsyncUtils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using System.Threading;

namespace ShowImages {
    static class Model {

        /// <summary>
        /// A solution for async image download operation.
        /// Try to describe why is the Unwrap necessary!
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

      
       



    }
}
