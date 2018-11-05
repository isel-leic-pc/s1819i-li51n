// From lecture at october, 31

package isel.leic.pc.lockfree;


import java.util.concurrent.atomic.AtomicReference;

public class StackLF<T> {
    private static class Node<T> {
        private final T value;
        // the volatile qualifier is really necessary?
        private volatile Node<T> next;
        private Node(T value) {
            this.value = value;
        }
    }

    // note that doesn't need the volatile qualifier
    // Atomic fields already behave as volatiles
    private AtomicReference<Node<T>> head = new AtomicReference<>(null);


    public void push(T value) {
        Node<T> nn = new Node<T>(value);
        while(true) {
            Node<T> obsHead = head.get();
            nn.next = obsHead;
            if (head.compareAndSet(obsHead, nn))
                return;
        }
    }

    public T pop() {
        while(true) {
            Node<T> obsHead = head.get();
            if (obsHead == null) return null;
            Node<T> headNext = obsHead.next;
            if (head.compareAndSet(obsHead, headNext))
                return obsHead.value;

        }
    }
}