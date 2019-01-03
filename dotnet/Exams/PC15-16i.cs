using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Testes {
    public class Uri { }
    
    public class Users {
        public interface Service {
            Task<int> FindIdAsync(String name, String birthdate);
            Task<Uri> ObtainAvatarUriAsync(int userId);
        }

        /// <summary>
        /// Esta solução é incorreta porque implica o bloqueio de threads 
        /// à espera do resultado das operações (porque invocam a propriedade "Result")
        /// </summary>
        /// <param name="svc"></param>
        /// <param name="name"></param>
        /// <param name="bdate"></param>
        /// <returns></returns>
        public static Task<Uri> GetUserAvatarAsyncBad(Service svc, String name, String bdate) {
            return Task.Run(() => {
                int userId = svc.FindIdAsync(name, bdate).Result;
                return svc.ObtainAvatarUriAsync(userId).Result;
            });
        }


        /// <summary>
        /// 1ª hipótese de solução, utilizando continuações
        /// </summary>
        /// <param name="svc"></param>
        /// <param name="name"></param>
        /// <param name="bdate"></param>
        /// <returns></returns>
        public static  Task<Uri> GetUserAvatar2Async(Service svc, String name, String bdate) {
            return svc.FindIdAsync(name, bdate).
                    ContinueWith(ant => {
                        int uid = ant.Result;
                        return svc.ObtainAvatarUriAsync(uid);
                    }).
                    Unwrap();
        }

        /// <summary>
        /// 2ª hipótese de solução, utilizando uma promessa
        /// É mais complicada porque obriga a lidar com eventuais excepções
        /// de modo a garantir que a promessa é sempre satisfeita
        /// </summary>
        /// <param name="svc"></param>
        /// <param name="name"></param>
        /// <param name="bdate"></param>
        /// <returns></returns>
        public static Task<Uri> GetUserAvatar3Async(Service svc, String name, String bdate) {
            var promise = new TaskCompletionSource<Uri>();
            svc.FindIdAsync(name, bdate).
                ContinueWith(ant => {
                    if (ant.IsFaulted) {
                        promise.SetException(ant.Exception.InnerException);
                        return;
                    }
                    int uid = ant.Result;
                    svc.ObtainAvatarUriAsync(uid).
                        ContinueWith(ant2 => {
                            if (ant2.IsFaulted) {
                                promise.SetException(ant.Exception.InnerException);
                                return;
                            }
                            promise.SetResult(ant2.Result);
                        });
                });
            return promise.Task;
        }

        /// <summary>
        /// 3ª hipótese, utilizando métodos assíncronos (async).
        /// De longe a mais simples!
        /// </summary>
        /// <param name="svc"></param>
        /// <param name="name"></param>
        /// <param name="bdate"></param>
        /// <returns></returns>
        public static async Task<Uri> GetUserAvatarAsync(Service svc, String name, String bdate) {
            var uid = await svc.FindIdAsync(name, bdate);
            return await svc.ObtainAvatarUriAsync(uid);
        }
    }

}
