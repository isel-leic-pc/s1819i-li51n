package isel.leic.pc.async.nio2;

import java.io.IOException;
import java.nio.ByteBuffer;
import java.nio.channels.AsynchronousFileChannel;
import java.nio.channels.AsynchronousSocketChannel;
import java.nio.channels.Channel;
import java.nio.channels.CompletionHandler;
import java.nio.charset.CharacterCodingException;
import java.nio.charset.Charset;
import java.nio.charset.CharsetDecoder;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.nio.file.StandardOpenOption;
import java.util.ArrayList;
import java.util.LinkedList;
import java.util.List;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.Future;
import java.util.concurrent.FutureTask;

public class FileUtils {

    private final static int BUFSIZE = 1024;

    private static void closeChannel(Channel c) {
        try { c.close(); } catch(Exception e) {}
    }

    private static void dispatchCompletion(CompletionHandler<String,Object> callback,
                                           String result, Object attach) {
        if (callback != null)  callback.completed(result, attach);
    }

    private static void dispatchFailure(CompletionHandler<String,Object> callback,
                                        Throwable error, Object attach) {
        if (callback != null)  callback.failed(error, attach);
    }

    /**
     * An example of NIO2 AsynchronousFileChannel using to
     * asynchronously get (using an CompletionHandler callbacks)
     * the start chars of a text file
     * @param fileIn
     * @param attach
     * @param callback
     * @throws IOException
     * @throws InterruptedException
     */
    public static void getHeadTextFileAsync(String fileIn, Object attach,
                                CompletionHandler<String,Object> callback)
            throws IOException, InterruptedException    {
        Path pathIn = Paths.get(fileIn);
        AsynchronousFileChannel channel =
                AsynchronousFileChannel.open(pathIn, StandardOpenOption.READ);
        ByteBuffer buffer = ByteBuffer.allocate(BUFSIZE);

        final Charset charSet = Charset.defaultCharset();
        final CharsetDecoder decoder = charSet.newDecoder();
        channel.read(buffer,0L,null,
                new CompletionHandler<Integer,Object>() {

            @Override
            public void completed(Integer result, Object attachment) {
                buffer.flip();
                try {
                    String content = decoder.decode(buffer).toString();
                    dispatchCompletion(callback, content, attach);
                }
                catch(CharacterCodingException e) {
                    callback.failed(e, attach);
                }
                closeChannel(channel);

            }

            @Override
            public void failed(Throwable exc, Object attachment) {
                dispatchFailure(callback, exc, attach);
                closeChannel(channel);
            }
        });
    }

    /**
     * Now an async file copy using NIO2 AsynchronousFileChannel
     * as an async operation that returns a CompletableFuture enable composition of asynchronous copies
     *
     * @param fileIn
     * @param fileOut
     * @return
     * @throws IOException
     */
    public static CompletableFuture<Long> copyFileAsync(String fileIn, String fileOut)
                                                                 throws IOException{
        CompletableFuture<Long> future = new CompletableFuture<>();

        Path pathIn = Paths.get(fileIn);
        Path pathOut = Paths.get(fileOut);
        ByteBuffer buffer = ByteBuffer.allocate(BUFSIZE);
        final AsynchronousFileChannel asyncFcIn =
                AsynchronousFileChannel.open(pathIn, StandardOpenOption.READ);
        final AsynchronousFileChannel asyncFcOut =
                AsynchronousFileChannel.open(pathOut,
                    StandardOpenOption.WRITE,
                    StandardOpenOption.TRUNCATE_EXISTING,
                    StandardOpenOption.CREATE);
        System.out.println("main thread: " + Thread.currentThread().getId());

        CompletionHandler<Integer, Long>[] writeCompletion = new CompletionHandler[1];

        CompletionHandler<Integer, Long> readCompletion = new CompletionHandler<Integer,Long>() {
            @Override
            public void completed(Integer result, Long attachment) {
                System.out.println("On read completed, current thread: " + Thread.currentThread().getId());
                if (result == 0) {
                    System.out.println("On read completed, closing fileChannels!");
                    closeChannel(asyncFcIn);
                    closeChannel(asyncFcOut);
                    future.complete(attachment);
                    return;
                }
                buffer.flip();
                asyncFcOut.write(buffer, attachment, attachment, writeCompletion[0] );
            }

            @Override
            public void failed(Throwable exc, Long attachment) {
                closeChannel(asyncFcIn);
                closeChannel(asyncFcOut);
                future.completeExceptionally(exc);
            }
        };

        writeCompletion[0] = new CompletionHandler<Integer,Long>() {
            @Override
            public void completed(Integer result, Long attachment) {
                System.out.println("On write completed, current thread: " +
                        Thread.currentThread().getId());
                buffer.flip();
                asyncFcIn.read(buffer, attachment+result,
                        attachment+result,readCompletion );
            }

            @Override
            public void failed(Throwable exc, Long attachment) {
                closeChannel(asyncFcIn);
                closeChannel(asyncFcOut);
                future.completeExceptionally(exc);
            }
        };
        asyncFcIn.read(buffer, 0, 0L, readCompletion);
        return future;
    }
}
