using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Aula_2018_10_31 {

    /// <summary>
    /// mutable range with SetLower and SetHigher maintaining the invariantes:
    ///     1-) low should not ever be higher than high.
    ///     2-) high should not be lower than low.
    /// </summary>
    public class Range {
        // immutable class used to guard the current range
        private class Pair {
            internal readonly int low, high;
            internal Pair(int low, int high) {
                this.low = low; this. high = high;
            }
        }

        private volatile Pair pair;

        public Range() {
            pair = new Pair(0,0);
        }

        public void SetLower(int lower) {
            Pair observed;
            do {
                observed = pair;
                if (lower > observed.high)
                    throw new InvalidOperationException("Lower cannot be higher than 'high'");
            }
            while (Interlocked.CompareExchange(
                ref pair, new Pair(lower, observed.high), observed) != observed);
     
        }

        public void SetHigher(int higher) {
            Pair observed;
            do {
                observed = pair;
                if (higher < observed.low)
                    throw new InvalidOperationException("Higher cannot be lower than 'low'");
            }
            while (Interlocked.CompareExchange(
                ref pair, new Pair(observed.low, higher), observed) != observed);

        }

    }
}
