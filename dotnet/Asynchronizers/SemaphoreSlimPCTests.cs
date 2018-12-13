using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Asynchronizers {


    public class SemaphoreSlimPCTests {
        [Test]
        public void SimpleAsyncAcquisitionTest() {
            SemaphoreSlimPC sem = new SemaphoreSlimPC(1, 1);
           
            var task = sem.AcquireAsync(CancellationToken.None, Timeout.Infinite);
            try { task.Wait(2000); } catch(Exception ) { }
            if (!task.IsCompleted || !task.Result) Assert.Fail();

            task = sem.AcquireAsync(CancellationToken.None, 5000);
            
            try {
                Assert.AreEqual(false, task.Result);
            }
            catch(AggregateException) {
                Assert.Fail();
            }     
        }

        [Test]
        public void SimpleCancellationTest() {
            SemaphoreSlimPC sem = new SemaphoreSlimPC(0, 1);
            CancellationTokenSource cts = new CancellationTokenSource();

            
            var task = sem.AcquireAsync(cts.Token, Timeout.Infinite);


            Task.Delay(5000).ContinueWith((ant) => {
                cts.Cancel();
            });
                 
            try {
                task.Wait();
                Assert.Fail();
            }
            catch (AggregateException exc) {
                Assert.AreEqual(typeof(TaskCanceledException), exc.InnerException.GetType());
                return;
            }
            Assert.Fail("Shouldn't come here");
        }
    }
}
