using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

/// <summary>
/// stack lock free from Treiber's algorithm
/// </summary>
namespace Aula_2018_10_31 {
    class StackLF<T> {
        private class Node {
            internal readonly T value;
            // it is really necessary mark the field as volatile?
            internal volatile Node next;

            internal Node(T value) {
                this.value = value;
            }
        }

        private volatile Node top;  // the top of the stack

        public void Push(T value) {
            Node observed;
            Node nn = new Node(value);
            do {
                observed = top;
                nn.next = observed;
            }
            while (Interlocked.CompareExchange(ref top, nn, observed) != observed);
        }

        /// <summary>
        /// Return success and the value, in order to be used with 
        /// reference and value types
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Pop(out T value) {
            Node observed;
            
            do {
                observed = top;
                if (observed == null) { value = default(T); return false; }
                
            }
            while (Interlocked.CompareExchange(ref top, observed.next, observed) != observed);
            value = observed.value;
            return true;
        }

    }
}
