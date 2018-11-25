using System;
using System.Text;
using ReflectionUtils;

/// <summary>
/// ThreaPoolMonitorConfiguration
/// 
/// Jorge Martins, 2017
/// </summary>

public class MyOptions : Options {
    private static int ACTION_COUNT = 200;
    private static int REPEAT_FOR = 50;
    private static int DEFAULT_BLOCK_TIME = 10; // in milliseconds
    private static int DEFAULT_EXEC_TIME = 5000000;  // in cycles
    private static int DEFAULT_WORK_INJECTION_PERIOD = 50; // in milliseconds

    [Option(Nickname = "a", Description = "set maximum worker items number")]
    public int nactions = ACTION_COUNT;

    [Option(Nickname = "r", Description = "set maximum repetitions on work item execution")]
    public int ntries = REPEAT_FOR;

    [Option(Nickname = "b", Description = "Use this to specify blocking work items")]
    public bool blocking = false;

    [Option(Nickname = "t", Description = "block time")]
    public int blockTime = DEFAULT_BLOCK_TIME;

    [Option(Nickname = "T", Description = "CPU execution time")]
    public int execTime = DEFAULT_EXEC_TIME;

    [Option(Nickname = "p", Description = "work injection period")]
    public int injectionPeriod = DEFAULT_WORK_INJECTION_PERIOD;


    public override String ToString() {
        StringBuilder sb = new StringBuilder();
        sb.Append("a=" + nactions + "\n");
        sb.Append("r=" + ntries + "\n");
        sb.Append("b=" + blocking + "\n");
        sb.Append("t=" + blockTime + "\n");
        sb.Append("T=" + execTime + "\n");
		sb.Append("p=" + injectionPeriod + "\n");
        return sb.ToString();
    }
}
 
