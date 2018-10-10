package isel.leic.pc.utils;

public class GenericBatch<T> {

    public static class Request<T> {
        public  T value;

        public Request(T val)  {
            value = val;
        }
    }

    private Request<T> current;

    private int size;

    public GenericBatch(T initial) {
        current = new Request<T>(initial);
        size=0;
    }

    public Request<T> add() {
        size++;
        return current;
    }

    public void remove(Request<T> current) {
        if (this.current != current || size ==0)
            throw new IllegalStateException();
        size--;
    }

    public Request<T>  current() {
        return current;
    }
    public void newBatch(T state) {
        current = new Request(state);
        size=0;
    }
}