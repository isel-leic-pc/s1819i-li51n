package isel.leic.pc.async.nio2;

import java.io.IOException;
import java.net.InetSocketAddress;
import java.nio.ByteBuffer;
import java.nio.channels.*;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * An asynchronous echo server using the nio2 socket channels
 */
public class AsyncEchoServer {
    private static final int BUFSIZE=4096;

    private static void closeChannel(Channel channel, int sessionId) {
        try {
            channel.close();
        }
        catch(IOException e) {
            System.out.printf("error closing channel: %s at session %d\n", e.getMessage(), sessionId);
        }
    }

    /**
     * The asynchronous session handler
     * @param channel
     * @param sessionId
     */
    public static void handle(AsynchronousSocketChannel channel, final int sessionId) {
        // The I/O BUFFER used in session
        ByteBuffer buffer = ByteBuffer.allocate(BUFSIZE);

        // The write completion callback container, to enable the dual recursive (read/write) cycle
        CompletionHandler<Integer, Object>[] cbWrite = new CompletionHandler[1];

        // the read completion callback
        CompletionHandler<Integer, Object> cbRead =  new CompletionHandler<Integer, Object>() {
            @Override
            public void completed(Integer result, Object attachment) {
                if (result <= 0) {
                    System.out.println("Connection closed with id "+ attachment);
                    closeChannel(channel, sessionId);
                    return;
                }
                buffer.flip(); // to rewind current index
                channel.write(buffer, sessionId, cbWrite[0]);
            }

            @Override
            public void failed(Throwable exc, Object attachment) {
                System.out.printf("error on channel read: %s at session %d\n", exc.getMessage(), sessionId);
                closeChannel(channel, sessionId);
            }
        };

        // the write completion callback
        cbWrite[0] = new CompletionHandler<Integer, Object>() {
            @Override
            public void completed(Integer result, Object attachment) {
                buffer.clear();
                channel.read(buffer, sessionId, cbRead);

            }

            @Override
            public void failed(Throwable exc, Object attachment) {
                System.out.printf("error on channel write: %s at session %d\n", exc.getMessage(), sessionId);
                closeChannel(channel, sessionId);
            }
        };

        // start the session with a first read
        channel.read(buffer, sessionId, cbRead);
    }

    public static void main(String[] args) {
        final int port = 8081;


        AtomicInteger sessionId = new AtomicInteger();
        try {
            AsynchronousServerSocketChannel serverSock =
                    AsynchronousServerSocketChannel.open().bind(new InetSocketAddress(port));

            CompletionHandler<AsynchronousSocketChannel, Integer>[] cb =
                    new CompletionHandler[1];
            cb[0] = new CompletionHandler<AsynchronousSocketChannel, Integer>() {

                @Override
                public void completed(AsynchronousSocketChannel result, Integer attachment) {
                    System.out.println("Connection created with id "+ attachment);
                    handle(result, attachment);
                    // start a new accept in order to enable concurrent sessions
                    serverSock.accept(sessionId.incrementAndGet(), cb[0]);
                }

                @Override
                public void failed(Throwable exc, Integer attachment) {
                    System.out.printf("error accepting session %d: %s\n",attachment, exc.getMessage());
                }
            };

            // start a first accept
            serverSock.accept(sessionId.incrementAndGet(), cb[0]);
            System.in.read();
            return;

        }
        catch(IOException e) {
            System.out.println("Fatal error: " + e.getMessage());
        }

    }
}
