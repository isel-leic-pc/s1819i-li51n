package isel.leic.pc.tests;

import isel.leic.pc.utils.LinkedList;
import org.junit.Test;
import static org.junit.Assert.*;

public class LinkedListTests {
    @Test
    public void retrieveFromEmptyListTest() {
        LinkedList<Integer> numSeq = new LinkedList<>();
        boolean  result = false;
        try {
            numSeq.removeFirst();
        }
        catch(IllegalStateException e) {
            result = true;
        }
        assertEquals(true, result);
    }

    @Test
    public void insertOnEmptyListTest() {
        LinkedList<Integer> numSeq = new LinkedList<>();

        LinkedList.Node<Integer> node = numSeq.add(1);
        assertEquals(1, numSeq.size());
        numSeq.remove(node);
        assertEquals(true, numSeq.isEmpty());
    }

    @Test
    public void insertOnNonEmptyListTest() {
        LinkedList<Integer> numSeq = new LinkedList<>();
        numSeq.add(1);

        LinkedList.Node<Integer> node = numSeq.add(2);
        assertEquals(2, numSeq.size());
        numSeq.remove(node);
        assertEquals(1, numSeq.size());
        assertEquals(new Integer(1), numSeq.getFirst());
    }
}
