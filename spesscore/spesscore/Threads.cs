using spesscore.VM;
namespace spesscore;

class LuaExecutionManager
{
    //SemaphoreSlim sync;
    List<Thread> ExecutionThreads = [];
    public Dictionary<int, Computer?> Running = [];

    Thread IntThread;

    static void Runner()
    {
        int i = 0;
        while (true)
        {
            if (SpessCore.Instance != null && SpessCore.Instance.Computers.Count > 0) {
                if (i >= SpessCore.Instance.Computers.Count) i = 0;
                var comp = SpessCore.Instance.Computers[i];
                if (comp != null && comp.Lock != null && comp.Lock.TryEnter()) {
                    if (comp.Deadline <= Times.CurTime)
                    {
                        comp.TryResume();
                    }
                    comp.Lock.Exit();
                }
                i++;
            } else
            {
                Thread.Sleep(500); // that way we're not burning CPU cycles
            }
        }
    }

    static void Inter()
    {
        while (true)
        {
            foreach (var pair in SpessCore.Instance.Manager.Running)
            {
                var comp = pair.Value;
                if (comp != null)
                {
                    lock(comp.pauselock) comp.Pause();
                }
                SpessCore.Instance.Manager.Running[pair.Key] = null;
            }
            Thread.Sleep(5); // this is the worst
        }
    }

    void AddThread()
    {
        Thread thd = new Thread(Runner);
        thd.Priority = ThreadPriority.Lowest;
        thd.IsBackground = true;
        thd.Name = "LuaExecutionManager "+ExecutionThreads.Count;
        ExecutionThreads.Add(thd);
    }

    public LuaExecutionManager()
    {
        //sync = new(Config.ThreadCount, Config.ThreadCount);
        for (int i=0; i < Config.ThreadCount; ++i)
        {
            AddThread();
        }
        IntThread = new Thread(Inter);
        IntThread.IsBackground = true;
        IntThread.Name = "Yerp 9001";
    }

    public void Start()
    {
        foreach (var thd in ExecutionThreads)
        {
            thd.Start();
        }
        IntThread.Start();
    }
}