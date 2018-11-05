// From lecture at october, 31

package isel.leic.pc.lockfree;

import java.util.concurrent.atomic.AtomicReference;

/// <summary>
/// mutable range with SetLower and SetHigher maintaining the invariantes:
///     1-) low should not ever be higher than high.
///     2-) high should not ever be lower than low.
/// </summary>
public class Range {

    // immutable class to mantain the current range
    private class Pair {
        private final int low, high;
        private Pair(int low, int high) {
            this.low = low; this. high = high;
        }
    }

    // note that doesn't need the volatile qualifier
    // Atomic fields already behave as volatiles
    private AtomicReference<Pair> pair;

    public Range() {
        pair = new AtomicReference<>(new Pair(0,0));
    }

    public void SetLower(int lower) {
        Pair observed;
        do {
            observed = pair.get();
            if (lower > observed.high)
                throw new IllegalStateException("Lower cannot be higher than 'high'");
        }
        while (!pair.compareAndSet(observed, new Pair(lower, observed.high)));
    }

    public void SetHigher(int higher) {
        Pair observed;
        do {
            observed = pair.get();
            if (higher < observed.low)
                throw new IllegalStateException("Higher cannot be lower than 'low'");
        }
        while (!pair.compareAndSet(observed, new Pair(observed.low, higher)));

    }

}