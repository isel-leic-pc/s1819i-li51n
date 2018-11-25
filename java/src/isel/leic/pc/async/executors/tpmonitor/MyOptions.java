package isel.leic.pc.async.executors.tpmonitor;

import isel.leic.pc.utils.optbuild.*;



public class MyOptions extends Options {
    private static int ACTION_COUNT = 100;
    private static int REPEAT_FOR = 50;
    private static int DEFAULT_BLOCK_TIME = 10; // in milliseconds
    private static int DEFAULT_EXEC_TIME = 5000000;  // in cycles
    private static int DEFAULT_QUEUE_SIZE = ACTION_COUNT;  // in cycles

    private static int DEFAULT_MIN_SIZE = Runtime.getRuntime().availableProcessors();
    private static int DEFAULT_MAX_SIZE = DEFAULT_MIN_SIZE*8;
    private static int DEFAULT_WORK_INJECTION_PERIOD = 50; // in milliseconds


    @Option(nickname = "S", description = "set maximum pool size")
    public int maxSize = DEFAULT_MAX_SIZE;

    @Option(nickname = "s", description = "set minimum pool size")
    public int minSize = DEFAULT_MIN_SIZE;


    @Option(nickname = "a", description = "set maximum worker items number")
    public int nactions = ACTION_COUNT;

    @Option(nickname = "r", description = "set maximum repetitions on work item execution")
    public int ntries = REPEAT_FOR;

    @Option(nickname = "b", description = "Use this to specify blocking work items")
    public boolean blocking = false;

    @Option(nickname = "t", description = "Use this to specify blocking time")
    public int blockTime = DEFAULT_BLOCK_TIME;

    @Option(nickname = "T", description = "Use this to specify cpu time")
    public int execTime = DEFAULT_EXEC_TIME;

    @Option(nickname = "q", description = "queue size")
    public int queueSize = DEFAULT_QUEUE_SIZE;

    @Option(nickname = "p", description = "work injection period")
    public int injectionPeriod = DEFAULT_WORK_INJECTION_PERIOD;

    public String toString() {
        StringBuilder sb = new StringBuilder();
        sb.append("a=" + nactions + "\n");
        sb.append("r=" + ntries + "\n");
        sb.append("b=" + blocking + "\n");
        sb.append("t=" + blockTime + "\n");
        sb.append("T=" + execTime + "\n");
        sb.append("q=" + queueSize + "\n");
        sb.append("p=" + injectionPeriod + "\n");
        sb.append("s=" + minSize + "\n");
        sb.append("S=" + maxSize + "\n");
        return sb.toString();
    }
}