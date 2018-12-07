package isel.leic.pc.async.nio2;

import com.sun.xml.internal.ws.policy.privateutil.PolicyUtils;

import java.io.IOException;
import java.nio.channels.CompletionHandler;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.Future;

public class FileUtilsTests {

    /**
     * The asynchronous copy file test
     */
    public static void asyncCopyTest() {
        try {
            CompletableFuture<Long> future = FileUtils.copyFileAsync("in.dat", "out.dat");
            future.whenComplete((l, t) -> {
                if (t==null)
                     System.out.println("Successfull copy: " + l + " bytes transfered!");
                else
                    System.out.println("Error on async copy: " + t.getMessage());

            });
        }
        catch(IOException e) {
            System.out.println("Error on async copy: " + e.getMessage());
        }
    }

    /**
     * The asynchronous show beginning of a file test
     */
    public static void showHeadTest() {
        String fileName = "src/isel/leic/pc/async/nio2/FileUtilsTests.java";
        try {
            FileUtils.getHeadTextFileAsync(fileName, null, new CompletionHandler<String,Object>() {
                @Override
                public void completed(String result, Object attachment) {
                    // show the start of the file in the callback
                    System.out.println(result);
                }

                @Override
                public void failed(Throwable exc, Object attachment) {
                    System.out.println("Error on showHead: " + exc.getMessage());
                }
            });
        }
        catch(Exception e) {
            System.out.println("Error on showHead start: " + e.getMessage());
        }
    }

    public static void main(String[] args) throws InterruptedException, IOException{
        System.out.println("Working Directory = " +
                System.getProperty("user.dir"));

        asyncCopyTest();
        //showHeadTest();

        // to avoid premature ending
        System.in.read();
    }
}
