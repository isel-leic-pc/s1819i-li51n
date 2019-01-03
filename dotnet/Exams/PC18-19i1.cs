using SynchUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Testes {
    /*
    2. [3 ] Implemente em Java ou C#, com base nos monitores implícitos ou explícitos, o sincronizador c hannel with
priority, para suportar a comunicação entre t hreads usando mensagens.A interface pública deste sincronizador é
a seguinte:
public class ChannelWithPriority<T> {
        public void Put(T msg, bool urgent);
        public bool Take(int timeout, out T rcvdMsg);
    }
    O método P ut e ntrega uma mensagem ao canal, sendo a sua prioridade definida com o argumento u rgent.
    Invocando o método T ake, a respectiva t hread manifesta a intenção de receber uma mensagem, ficando
    bloqueada se não existir nenhuma mensagem disponível.O método T ake t ermina: retornando t rue, quando é
    recebida uma mensagem, sendo esta devolvida através do parâmetro r cvdMsg; retornando f alse, se expirar
o limite do tempo de espera, ou; lançando T hreadInterruptedException quando a espera da t hread é
interrompida.As mensagens deve ser entregues às t hreads consumidoras tendo em consideração a sua
prioridade(urgente ou normal) e para cada prioridade a ordem com que foram entregues ao canal(FIFO). As
threads consumidoras devem ser servidas pela ordem inversa da invocação do método Take(LIFO).
Nota: Se implementar o sincronizador em Java altere adequadamente a assinatura do método Take
*/
    public class KeyedChannel<T,K> where T: class{
        private object monitor;

        Dictionary<K, LinkedList<T>> msgs;

        Dictionary<K, LinkedList<T>> waiters;

        public KeyedChannel() {
            monitor = new object();
            msgs = new Dictionary<K, LinkedList<T>>();
            waiters = new Dictionary<K, LinkedList<T>>();
        }

        private LinkedList<T> getKeyWaiters(K key) {
            LinkedList<T> kw;
            if (!waiters.TryGetValue(key, out kw)) return null;
            return kw;
        }

        private LinkedList<T> getKeyMsgs(K key) {
            LinkedList<T> km;
            if (!msgs.TryGetValue(key, out km)) return null;
            return km;
        }

        private LinkedListNode<T> putKeyWaiter(K key) {
            LinkedList<T> kw;
            if (!waiters.TryGetValue(key, out kw))
                waiters.Add(key, kw = new LinkedList<T>());
            return kw.AddLast((T)null);
        }

        void Put(T msg, K key) {
            lock(monitor) {
                var kw = getKeyWaiters(key);
                if (kw.Count > 0) {
                    var node = kw.First;
                    kw.RemoveFirst();
                    node.Value = msg;
                    monitor.Notify(node);
                }
            }
        }

        T Take(K key) {
            lock(monitor) {
                var km = getKeyMsgs(key);
                if (km != null) {
                    var mn = km.First;
                    km.RemoveFirst();
                    return mn.Value; 
                }
               
                var node = putKeyWaiter(key);
               
                while(true) {
                    try {
                        monitor.Wait(node);
                        if (node.Value != null) return node.Value;
                    }
                    catch(ThreadInterruptedException ) {
                        if (node.Value != null) {
                            Thread.CurrentThread.Interrupt();
                            return node.Value;
                        }
                        throw;
                    }
                }
            }
        }
    }

    public class TPLUtils {
        public static R TextAnalyser<R>(
            IEnumerable<String> text, Func<String,R> processor,  Func<R,R,R> agregator, Func<R,bool> goal, R start)  {
            R result = start;
            foreach (var str in text) {
                result = agregator(result, processor(str));
                if (goal(result)) break;
            }
            return result;
        }
    }
}
