using NUnit.Framework;
using System;
 
using System.Threading;
using Aula_2017_10_26.LockFree;

namespace Aula_2017_10_26.Tests {
    public class ManualResetEventTests {
        private const int PARTS = 2;
        private const int WAITERS = PARTS - 1;
        private const int NROUNDS = 500;
       
        private const int WAIT_ROUND_TIMEOUT = 1000;

        private class Result {
            internal volatile bool exception;
            
            internal int[] waitScores = new int[WAITERS];
            internal int setterScores;

            public bool ok() {
                return !exception;
                      
            }
        }


        private volatile CountdownEvent cdl;

        [Test]
        public void multipleWaitersTest() {
           
            Barrier cb = new Barrier(PARTS);
            Thread[] threads = new Thread[WAITERS];
            Result res = new Result();
            var mre = new LockFree.ManualResetEvent(false);

            for (int i = 0; i < WAITERS; ++i) {
                int li = i;
                Thread t = new Thread(() => {
                    for (int r = 0; r < NROUNDS && res.ok(); ++r) {
                        try {
                            cb.SignalAndWait(WAIT_ROUND_TIMEOUT);
                            mre.await(Timeout.Infinite);
                            cdl.Signal();
                            res.waitScores[li]++;
                        }
                        catch (Exception  ) {
                            res.exception = true;
                        }
                    }
                });
                t.Start();
                threads[i] = t;
            }

            for (int r = 0; r < NROUNDS && res.ok(); ++r) {
                try {
                    cb.SignalAndWait(WAIT_ROUND_TIMEOUT);
                    cdl = new CountdownEvent(WAITERS);
                    mre.set();
                    if (!cdl.Wait(WAIT_ROUND_TIMEOUT))
                        res.exception = true;

                    res.setterScores++;
                    mre.reset();
                }
                catch (Exception  ) {
                   res.exception = true;
                }
            }

            bool okWaiterCounters = res.ok();
            for (int w = 0; w < WAITERS && okWaiterCounters; ++w) {
              
                threads[w].Join();
                okWaiterCounters = okWaiterCounters && res.waitScores[w] == NROUNDS;
        
            }


            Assert.IsFalse(res.exception);
            Assert.IsTrue(okWaiterCounters && res.setterScores == NROUNDS);
            Assert.IsTrue(mre.getWaiters() == 0);
        }

    }
}
