using NUnit.Framework;
using System.Threading;

namespace Aula_2018_09_25 {
    [TestFixture]
    public class CounterSemaphoreFIFOTests {
        [Test]
        public void NonBlockingAcquireTest() {
            CounterSemaphoreFIFO sem = new CounterSemaphoreFIFO(10);
            bool result = sem.Acquire(5, 1000);
            Assert.AreEqual(true, result);
        }

        [Test]
        public void ImmediateFailureAcquireTest() {
            CounterSemaphoreFIFO sem = new CounterSemaphoreFIFO(1);
            bool result = sem.Acquire(5, 0);
            Assert.AreEqual(false, result);
        }

        [Test]
        public void FailureByTimeoutAcquireTest() {
            CounterSemaphoreFIFO sem = new CounterSemaphoreFIFO(1);
            bool result = false;

            Thread t = new Thread(() => {
                try {
                    bool res = sem.Acquire(5, 1000);
                    result = !res;
                }
                catch (ThreadInterruptedException) {

                }
            });
            t.Start();

            // wait for thread termination with timeout
            // in order to made the test robust
            t.Join(5000);

            Assert.AreEqual(true, result);
        }

    }
}
