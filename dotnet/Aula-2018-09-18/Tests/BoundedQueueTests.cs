using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Aula_2018_09_18.Tests {

    [TestFixture]
    public class BoundedQueueTests {

        [Test]
        public void OneProducerOneConsumerTest() {
            const int CAPACITY = 100;
            const int MAX_VAL = 100000;
            const int CONCLUDED_TIMEOUT = 10000;

            ManualResetEvent concluded = new ManualResetEvent(false);
            BoundedQueue<int> queue = new BoundedQueue<int>(CAPACITY);
            bool result = true;

            ThreadStart producer = () => {
                try {
                    for (int i = 1; i <= MAX_VAL; ++i) {
                        queue.Put(i);

                    }
                }
                catch (Exception e) {
                    result = false;
                }
            };

            ThreadStart consumer = () => {
                int pred = 0;
                try {
                    for (int i = 1; i <= MAX_VAL; ++i) {

                        int val = queue.Get();
                        if (val != pred + 1) {
                            result = false;
                            return;
                        }

                        pred = val;

                    }
                }
                catch (Exception e) {
                    result = false;
                }
                finally {
                    concluded.Set();
                }


            };

            Thread tprod = new Thread(producer);
            tprod.Start();

            Thread tcons = new Thread(consumer);
            tcons.Start();

            bool waitRes = concluded.WaitOne(CONCLUDED_TIMEOUT);
            concluded.Close();


            Assert.AreEqual(true, result, "Error on Get!");
            Assert.AreEqual(true, waitRes, "Timeout error!");
        }

        [Test]
        public void OneProducerOneConsumerBlockedOnGet() {
            const int CAPACITY = 100;
            const int MAX_VAL = 10000;
            const int CONCLUDED_TIMEOUT = 5000;

            ManualResetEvent concluded = new ManualResetEvent(false);
            BoundedQueue<int> queue = new BoundedQueue<int>(CAPACITY);
            bool result = true;

            ThreadStart producer = () => {
                for (int i = 1; i <= MAX_VAL; ++i)  
                    queue.Put(i);
            };

            ThreadStart consumer = () => {
                int pred = 0;
                try {
                    // read one more than is produced
                    for (int i = 1; i <= MAX_VAL + 1; ++i) {

                        int val = queue.Get();
                        if (val != pred + 1) {
                            result = false;
                            return;
                        }
                        pred = val;
                    }
                }
                catch (Exception e) {
                    result = false;
                }
                finally {
                    concluded.Set();
                }
            };

            Thread tprod = new Thread(producer);
            tprod.Start();

            Thread tcons = new Thread(consumer);
            tcons.Start();

            bool waitRes = concluded.WaitOne(CONCLUDED_TIMEOUT);
            concluded.Close();

            Assert.AreEqual(true, result, "Error on Get!");
            Assert.AreEqual(false, waitRes, "Timeout must occurr!");
        }

        private bool allTrue(bool[] values) {
            return !values.Any(v => v == false);
        }

        private bool CheckRange(int min, int max, int val) {
            return (val >= min && val < max);
        }

        private void BurstProducerManyConsumersTestImpl(IBoundedQueue<int> queue) {


            const int CONCLUDED_TIMEOUT = 10000;
            int capacity = queue.Capacity;

            var concluded = new ManualResetEvent(false);
            bool[] values = new bool[capacity];

            ThreadStart producer = () => {
                Random random = new Random();
                for (int v = 0; v < capacity; ++v) {
                    queue.Put(v);
                }
            };

            ThreadStart consumer = () => {

                int val = queue.Get();

                if (CheckRange(0, capacity, val)) {
                    values[val] = true;

                    if (allTrue(values))
                        concluded.Set();
                }


            };

            for (int i = 0; i < capacity; ++i) {
                new Thread(consumer).Start();
            }


            new Thread(producer).Start();

            bool result = concluded.WaitOne(CONCLUDED_TIMEOUT);
            concluded.Close();
            Assert.AreEqual(true, result);
        }


        [Test]
        public void BurstProducerManyConsumersTest() {
            const int CAPACITY = 100;
            var queue = new BoundedQueue<int>(CAPACITY);


            BurstProducerManyConsumersTestImpl(queue);
        }

        [Test]
        public void BurstProducerManyConsumersMRETest() {
            const int CAPACITY = 100;
            var queue = new BoundedQueueMRE<int>(CAPACITY);


            BurstProducerManyConsumersTestImpl(queue);
        }
    }
}
