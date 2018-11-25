using Exercicios_Parte2;
using NUnit.Framework;
using System;
using System.Threading;

namespace SeriesSynchTests
{
    public class SeriesSinchTests
    {

        private void TerminateThread(Thread t) {
            try { t.Abort(); } catch (Exception) { }
        }
        // SafeSempahoreTests

        [Test]
        public void TwoAcquiresOnOnePermitsSemaphoreTest() {
            int acquire1 = -1, acquire2 = -1;
            Thread t = new Thread(() => {
                SafeSemaphore sem = new SafeSemaphore(1, 1);

                acquire1 = sem.TryAcquire(1) ? 1 : 0;
                acquire2 = sem.TryAcquire(1) ? 1 : 0;
            });
            t.Start();

            bool done = t.Join(2000);

            if (!done) {
                TerminateThread(t);
                Assert.Fail();             
            }
            Assert.AreEqual(1, acquire1);
            Assert.AreEqual(0, acquire2);
        }

        [Test]
        public void MultipleAcquiresSemaphoreTest() {
            const int ACQUIRES = 10;

            bool[] acquires = new bool[ACQUIRES];
            Thread[] threads = new Thread[ACQUIRES];
            SafeSemaphore sem = new SafeSemaphore(10, 10);

            for (int i = 0; i < ACQUIRES; ++i) {
                threads[i] = new Thread((idx) => {
                    bool res;
                    do {
                        res = sem.TryAcquire(1);
                    }
                    while (!res);
                    acquires[(int)idx] = true;
                });
                threads[i].Start(i);
            }

            // check if all threads terminate
            bool result = true;
            for (int i = 0; i < ACQUIRES; ++i) {
                bool partial = threads[i].Join(2000);
                if (!partial) TerminateThread(threads[i]);
                result = result && partial;
            }
            if (!result) Assert.Fail("One or more threads not terminated!");

            // check if all results are true
            for (int i = 0; i < ACQUIRES; ++i) {
                if (!acquires[i]) Assert.Fail("Unsuccessful acquire!");
            }

            Assert.AreEqual(false, sem.TryAcquire(1));
        }

    }
}
