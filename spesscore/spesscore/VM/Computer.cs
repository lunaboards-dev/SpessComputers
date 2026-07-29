using System.Runtime.InteropServices;
using spesscore.VM.Peripheral;

using static spesscore.VM.Lua;
using static spesscore.VM.Helpers;
using System.Text;
using Cyotek.Collections.Generic;
using spesscore.VM.Libraries;
using spesscore.VM.Peripheral.Disk;

namespace spesscore.VM;

class Computer
{
    public delegate int IOYieldCallback(lua_State L);
    IOYieldCallback? ioyield;
    protected internal int max_memory = 1024*1024;
    protected internal int currently_allocated = 0;
    protected internal List<IPeripheral> Peripherals = [];
    protected internal CircularBuffer<LuaSignal> events = new(Config.EventBufferSize, true);
    protected internal TTY? LocalTTY;
    public TTY? LocalTerminal => LocalTTY;
    public EEPROM? eeprom;
    public ManagedDisk? Disk;
    // this really should be a bitfield
    protected internal bool running = false;
    protected internal bool init_once = false;
    protected internal bool paused = false;
    protected internal bool iowait = false;
    protected internal bool iores = false;
    public int SignalCount => events.Size;
    public readonly Lock pauselock = new();
    public readonly Lock Lock = new();
    protected internal readonly Lock PLL = new();
    public int strikes = 0;
    public double exec_deadline = 0;
    public double exec_hardline = 0;
    public int punishment = 0;
    ComputerLib lib;

    lua_Alloc MemAlloc;
    lua_Hook PauseExecDel;

    public double Deadline;

    protected internal lua_State PL;
    protected internal lua_State L;
    protected internal lua_State BL;

    // cache delegates here
    public Computer()
    {
        MemAlloc = Allocator;
        PauseExecDel = PauseExecution;
        lib = new(this);
    }

    public static void DumpStack(lua_State L)
    {
        int size = lua_gettop(L);
        Console.WriteLine($"!! STACK DUMP START ({size})");
        for (int i=0;i<size;++i)
        {
            string? s = lua_tostring(L, 1);//l.ToString(i+1);
            string? t = luaL_typename(L, 1);//l.TypeName(i+1);
            Console.WriteLine($"[{i}]: ({t})\t{s}");
        }
        Console.WriteLine("!! STACK DUMP END");
    }

    public static int DumpStackL(lua_State L)
    {
        DumpStack(L);
        return 0;
    }

    // lol we don't even have to change this
    unsafe nuint Allocator(lua_State ud, nuint ptr, ulong osize, ulong nsize)
    {
        nint delta = ((int)nsize)-((int)osize);
        if (delta+currently_allocated > max_memory)
        {
            Console.WriteLine("OOM");
            return 0; // wrong, chlorine trifluoride
        }
        void* p = NativeMemory.Realloc((void*)ptr, (nuint)nsize);
        currently_allocated+=(int)delta;
        return (nuint)p;
    }

    List<lua_CFunction> DelegateTrack = []; // this is the secret sauce to stop the GC from eating us

    /* static LuaFunction Recaller(CSFunc f)
    {
        return (ptr) =>
        {
            Lua ctx = Lua.FromIntPtr(ptr);
            return f(ctx);
        };
    } */

    void InitLuaState()
    {
        if (init_once)
        {
            lua_close(L);
        }
        init_once = true;
        L = lua_newstate(MemAlloc, 0);//new Lua(Allocator, 0);
        //L = lua_newthread(PL);
        luaL_openlibs(L);
        lib.Push(L);
        //QueryReader.InitLib(L);
    }

    static lua_CFunction PerCallDel = PeripheralCall;
    protected internal static int PeripheralCall(lua_State L)
    {
        Computer? c = lua_ToObject<Computer>(L, lua_upvalueindex(1));
        IPeripheral? p = lua_ToObject<IPeripheral>(L, lua_upvalueindex(2));
        //IPeripheral.PeripheralCallback cb = lua_ToObject<IPeripheral.PeripheralCallback>(L, lua_upvalueindex(3));
        string? method = lua_tostring(L, lua_upvalueindex(3));
        if (c == null || p == null || method == null)
            return luaL_error(L, "internal error");
        if (p.Computer != c)
        {
            return luaL_error(L, "not connected");
        }
        if (!p.Callbacks.TryGetValue(method, out IPeripheral.PeripheralCallback? value))
        {
            return luaL_error(L, "invalid callback");
        }
        return value(L);
    }

    protected internal void PushPeripheral<T>(lua_State l, T p) where T : IPeripheral
    {
        lua_PushObjectManaged(L, p);
        lua_newtable(L);
        //l.PushString("__tostring");
        /* l.PushCFunction((ptr) =>
        {
            Lua ctx = Lua.FromIntPtr(ptr);
            ctx.PushString(p.GetType().FullName);
            return 1;
        });
        l.SetTable(-3);*/
        lua_pushstring(L, "__index");
        lua_newtable(L);
        foreach (var method in p.Callbacks)
        {
            lua_pushstring(L, method.Key);
            lua_PushTemporaryObject(L, this);
            lua_PushTemporaryObject(L, p);
            lua_pushstring(L, method.Key);
            lua_pushcclosure(L, PerCallDel, 3);
            lua_settable(L, -3);
        }
        lua_settable(L, -3);

        lua_pushstring(L, "__gc");
        lua_pushcfunction(L, ReleaseObjectDelegate);
        lua_settable(L, -3);

        lua_setmetatable(L, -2);
    }

    public void MemoryResize(int newsize)
    {
        if (currently_allocated > newsize)
        {
            luaL_error(L, "Out of memory");
        }
        max_memory = newsize;
    }

    public List<IPeripheral> GetPeripherals(string type)
    {
        return Peripherals.Where((comp)=>comp.PeripheralName == type).ToList();
    }

    public IPeripheral? GetPeripheral(string id)
    {
        foreach (IPeripheral comp in Peripherals)
        {
            if (comp.ID == id)
                return comp;
        }
        return null;
    }

    public void AddPeripheral(IPeripheral p)
    {
        p.Attach(this);
        Peripherals.Add(p);
    }

    public void SetLocalTTY(string id)
    {
        var p = GetPeripheral(id);
        if (p != null && p is TTY tty)
        {
            LocalTTY = tty;
        }
    }
    bool active = false;
    public bool Active => active;
    public void Pause()
    {
        if (active && !paused) {
            paused = true;
            //Console.WriteLine("Pausing");
            lock (PLL) lua_sethook(PL, PauseExecDel, LUA_MASKCOUNT, 1); // this SHOULD be thread safe, believe it or not
        }
    }

    public int Yield(lua_State L)
    {
        Deadline = Times.CurTime+0.005;
        paused = true;
        return lua_yield(L, 0);
    }

    // STAHP! NO!
    public void Stop()
    {
        Pause();
        active = false;
    }

    static lua_CFunction BwoinkDel = BwoinkCollect;
    static int BwoinkCollect(lua_State L)
    {
        string str = lua_tostring(L, 1);
        luaL_traceback(L, L, str, 1);
        string traceback = lua_tostring(L, -1);
        SpessCore.Instance?.Bwoink(traceback);
        //Console.WriteLine("Uncaught error: "+traceback);
        return 0;
    }
    void PauseExecution(lua_State L, lua_Debug ar)
    {
        //Console.WriteLine("STOP EXEC");
        //Console.WriteLine($"Paused in {ar.currentline}");
        Yield(L);
        lua_sethook(L, null, 0, 0);
        lock(PLL) iores = false;
        //DumpStack(L);
    }

    public void Start()
    {
        try {
            InitLuaState(); // oh my fucking god bruh
            //lua_sethook(L, PauseExecDel, LUA_MASKCOUNT, 5000);
/*             lua_PushTemporaryObject(L, this);
            lua_pushcclosure(L, BwoinkDel, 1); */
            //luaL_loadstring(L, Encoding.UTF8.GetString(SpessCore.Instance?.MachineLua));
            luaL_loadbufferx(L, SpessCore.Instance.MachineLua, (uint)SpessCore.Instance.MachineLua.Length, "=machine.lua", "t");
            if (lua_type(L, -1) != LUA_TFUNCTION)
            {
                throw new Exception("Failed to load machine.lua: "+lua_tostring(L, -1));
            }
            DumpStack(L);
            //L.LoadBuffer(SpessCore.Instance?.MachineLua, "=machine.lua");
            //lua_pcall(L, 0, 0, -2);//L.PCall(0, 0, -2);
            int c = 0;
            lock(PLL) PL = L;
            lua_resume(L, 0, 0, ref c);
            lua_pop(L, c);
            lua_gc(BL, LUA_GCCOLLECT);
            active = true;
        } catch (Exception e)
        {
            Console.Write(e);
            return;
        }
        active = true;
    }

    bool Resume()
    {
        //Console.WriteLine("RESUME");
        int remove = 0;
        lock(pauselock) {
            //lua_sethook(L, PauseExecDel, 0, 0);
            paused = false;
        }
        int rtv = 0;
        if (ioyield != null)
        {
            rtv = ioyield(L);
            ioyield = null;
        }
        exec_deadline = Times.CurTime + Config.ContextSwitchTime;
        exec_hardline = Times.CurTime + (Config.ContextSwitchTime * 10); // pass this and you will DIE
        int state = lua_resume(L, 0, rtv, ref remove);
        Console.WriteLine("Yield VM");
        if (postrun != null)
        {
            postrun();
            postrun = null;
        }
        Console.WriteLine("Ran postrun");
        double end_time = Times.CurTime;
        if (end_time >= exec_hardline)
        {
            LocalTTY?.Write("Watchdog triggered!");
            Console.WriteLine("Watchdog triggered");
            return true; // NOT INTO THE PIT, IT BURNS
        }
        bool dead = state != LUA_YIELD;
        if (dead && state != LUA_OK)
        {
            string err = lua_tostring(L, -1);
            LocalTTY?.Write("FAILED TO RESUME: "+err);
        }
        DumpStack(L);
        lua_pop(L, remove);
        DumpStack(L);
        // calculate our punishment (reaper thread cycles before we're allowed to resume)
        punishment = (int)Math.Floor((end_time-exec_deadline)/Config.ContextSwitchTime);
        Console.WriteLine("Punishment: "+punishment);
        return dead;
    }

    public void TogglePower(bool hard)
    {
        if (active)
        {
            if (hard)
            {
                Stop();
            } else
            {
                // push signal to VM
            }
        } else {
            Start();
        }
    }

    public void TryResume()
    {
        Thread.BeginCriticalRegion();
        if (active && !running && !iowait)
        {
            running = true;
            SpessCore.Instance.Manager.Running[Thread.CurrentThread.ManagedThreadId] = this;
            Thread.EndCriticalRegion();
            try
            {

                if (Resume())
                {
                    active = false; // he ded
                }
            }
            catch (Exception)
            {
                // handle lol
            }
            running = false;
        } else
        {
            //Console.WriteLine($"active: {active}, running: {running}, iowait: {iowait}");
            Thread.EndCriticalRegion();
        }
    }

    public void PushSignal(LuaSignal signal)
    {
        events.Put(signal);
    }

    Func<Task>? postrun = null;

    public int EnterIOWait(IOYieldCallback callback, Func<Task> run)
    {
        Console.WriteLine("Enter I/O Wait");
            iowait = true;
            paused = true;
            ioyield = callback;
            postrun = run;
            Console.WriteLine("End lock(pauselock)");
            return lua_yield(L, 0);
    }

    public void ExitIOWait()
    {

        Console.WriteLine("Begin exit I/O wait");
        lock (pauselock)
        {
            iowait = false;
            paused = false;
            iores = true;
        }
    }

    ~Computer()
    {
        lua_close(L);
    }
}