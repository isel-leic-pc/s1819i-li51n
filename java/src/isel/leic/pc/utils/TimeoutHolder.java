package isel.leic.pc.utils;

public class TimeoutHolder {
    private long expired;
    public static final long INFINITE = -1;

    public TimeoutHolder(long millis) {
        if (millis == INFINITE) expired =0;
        else expired = System.currentTimeMillis() + millis;
    }

    public boolean isTimed() {
        return expired != 0;
    }

    public long value() {
        if (!isTimed()) return Long.MAX_VALUE;
        return Math.max(0, expired - System.currentTimeMillis());
    }
}
