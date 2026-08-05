// contains all core VM stuff
using spesscore.VM.Libraries;
using static spesscore.VM.Lua;
using static spesscore.VM.Helpers;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace spesscore.VM;

class VMCore
{
    // main lock
    public Lock Lock = new();
    List<Library> Libs = [];
    bool LuaInit = false;
    public lua_State L;
    public lua_State TL;

    public Lock LuaLock = new(); // required for accessing either L or TL.

    public double ExecSoftDeadline;
    public double ExecHardDeadline;
    public double ResumeDeadline;
    int Punishment = 0;
    public int Punish => Punishment;
    public Lock TimingLock = new();
    int State = 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool StateTest(VMState s)
    {
        int t = (int)s;
        Interlocked.And(ref t, State);
        return t > 0;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void StateClear(VMState s)
    {
        int t = 0xFF ^ (int)s;
        Interlocked.And(ref State, t);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void StateSet(VMState s)
    {
        int t = (int)s;
        Interlocked.Or(ref State, t);
    }

    public bool Active => StateTest(VMState.Active);
    public bool Paused => StateTest(VMState.Paused);
    public bool IOWait => StateTest(VMState.IOWait);
    public bool Running => StateTest(VMState.Running);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnterCritical()
    {
        StateSet(VMState.Critical);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ExitCritical()
    {
        StateClear(VMState.Critical);
    }

    byte[] StartupCode;

    public event Action<string> OnError;
    public event Action OnWatchdog;

    public VMCore(byte[] startup)
    {
        StartupCode = startup;
        PauseExecDel = PauseExecution;
    }

    lua_Hook PauseExecDel;
    void PauseExecution(lua_State L, lua_Debug ar)
    {
        //Console.WriteLine("STOP EXEC");
        //Console.WriteLine($"Paused in {ar.currentline}");
        Yield(L);
        lock(LuaLock) lua_sethook(L, null, 0, 0); // oh we should probably lock this
        StateClear(VMState.Running);
        //DumpStack(L);
    }

    void UpdateDeadlines()
    {
        // lock, just in case
        lock (TimingLock)
        {
            ExecHardDeadline = Times.CurTime + (Config.ContextSwitchTime*100);
            ExecSoftDeadline = Times.CurTime + Config.ContextSwitchTime;
        }
    }

    public void DecPunish()
    {
        Interlocked.Decrement(ref Punishment);
    }

    public bool ShouldRun()
    {
        return StateTest(VMState.Active) && !StateTest(VMState.IOWait | VMState.Paused);
    }

    // used externally, forces a preempt yield.
    public void Pause()
    {
        if (StateTest(VMState.Active) && !Paused)
        {
            StateSet(VMState.Paused);
            lock(LuaLock) lua_sethook(TL, PauseExecDel, LUA_MASKCOUNT, 1);
        }
    }

    // used internally, forces a generic yield.
    public int Yield(lua_State L, bool as_preempt=false)
    {
        // don't actually yield if we're in a critical function, unless we've hit the hard deadline
        if (StateTest(VMState.Critical) && (Times.CurTime < ExecHardDeadline)) return 0;
        if (as_preempt)
        {
            StateSet(VMState.Paused);
        }
        return lua_yield(L, 0);
    }

    public bool Resume(bool IgnoreDeadline=false)
    {
        //Console.WriteLine("RESUME");
        StateSet(VMState.Running);
        UpdateDeadlines();
        StateClear(VMState.Paused | VMState.IOWait);
        int count = 0;
        int status = lua_resume(L, 0, 0, ref count);
        double end_time = Times.CurTime;
        double times = (end_time-ExecSoftDeadline)/Config.ContextSwitchTime;
        if (end_time >= ExecHardDeadline && !IgnoreDeadline)
        {
            OnWatchdog?.Invoke();
            Console.WriteLine($"Watchdog triggered ({times}x limit)");
            return true; // NOT INTO THE PIT, IT BURNS
        }
        bool dead = status != LUA_YIELD;
        if (dead)
        {
            StateClear(VMState.Active);
        }
        if (dead && status != LUA_OK)
        {
            string err = lua_tostring(L, -1);
            OnError?.Invoke(err);
        }
        lua_pop(L, count);
        StateClear(VMState.Running);
        Punishment = (int)Math.Floor(times);
        return dead;
    }

    public nint CurrentAlloc = 0;
    public nint MaxMemory = 0;
    unsafe nuint Allocator(lua_State ud, nuint ptr, ulong osize, ulong nsize)
    {
        nint delta = ((int)nsize)-((int)osize);
        if (delta+CurrentAlloc > MaxMemory)
        {
            Console.WriteLine("OOM");
            return 0; // wrong, chlorine trifluoride
        }
        void* p = NativeMemory.Realloc((void*)ptr, (nuint)nsize);
        CurrentAlloc+=(int)delta;
        return (nuint)p;
    }
    lua_Alloc AllocatorDel;

    void InitLuaState()
    {
        lock(LuaLock) {
            if (LuaInit)
            {
                lua_close(L);
            }
            LuaInit = true;
            AllocatorDel = Allocator;
            L = lua_newstate(AllocatorDel, 0);
            TL = L;
            luaL_openlibs(L);
            foreach (var lib in Libs)
            {
                lib.Push(L);
            }
            luaL_loadbufferx(L, StartupCode, (uint)StartupCode.Length, "=machine.lua", "t");
            if (lua_type(L, -1) != LUA_TFUNCTION)
            {
                throw new Exception("Failed to load machine.lua: "+lua_tostring(L, -1));
            }
        }
    }

    public bool TryEnter()
    {
        if (!Active || IOWait || Running) return false;
        return Lock.TryEnter();
    }

    public void Exit()
    {
        Lock.Exit();
    }

    public void Start()
    {
        InitLuaState();
        StateSet(VMState.Active);
    }

    public void AddLibrary(Library lib)
    {
        Libs.Add(lib);
    }

    internal void Stop()
    {
        Pause();
        StateClear(VMState.Active);
    }

    public bool TryResume()
    {
        if (!Resume())
        {
            StateClear(VMState.Active);
            return false;
        }
        return true;
    }
}