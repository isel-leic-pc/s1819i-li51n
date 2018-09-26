package isel.leic.pc.utils;

public class LinkedList<T>  {

    public static class Node<T> {
        public final T value;
        private Node next, previous;

        private Node() {
            value = null;
            next = previous = this;
        }

        public Node(T value) {
            this.value = value;
        }
    }

    private Node<T> head;
    private int count;

    public LinkedList() {
        head = new Node<T>();
    }

    public Node<T> add(T value) { // insert value at tail
        Node<T> n = new Node<T>(value);
        add(n);
        return n;
    }

    private void add(Node n) { // insert node at tail
        Node previous = head.previous;
        n.next = previous.next;
        n.previous = previous;
        previous.next = n;
        head.previous = n;
        count++;
    }

    public void remove(Node n) {
        n.previous.next = n.next;
        n.next.previous = n.previous;
        count--;
    }

    public int size() {
        return count;
    }

    public boolean isEmpty() {
        return head.next == head && head.previous == head;
    }

    public T removeFirst() {
        if (size() == 0) throw new IllegalStateException();
        Node<T> node = head.next;
        remove(node);
        return node.value;
    }

    public T getFirst() {
        if (size() == 0) throw new IllegalStateException();
        Node<T> n = head.next;
        return n.value;
    }
}
