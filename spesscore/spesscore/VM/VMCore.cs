// contains all core VM stuff
using spesscore.VM.Libraries;
using static spesscore.VM.Lua;
using static spesscore.VM.Helpers;
using System.Runtime.InteropServices;

namespace spesscore.VM;

class VMCore(byte[] startup)
{
    // main lock
    Lock Lock = new();
    List<Library> Libs = [];
    bool LuaInit = false;
    lua_State L;
    lua_State TL;

    Lock LuaLock = new(); // required for accessing either L or TL.

    public double ExecSoftDeadline;
    public double ExecHardDeadline;
    public double ResumeDeadline;
    int Punishment = 0;
    public int Punish => Punishment;
    Lock TimingLock = new();
    int State = 0;
    byte[] StartupCode = startup;

    public event Action<string> OnError;
    public event Action OnWatchdog;

    void UpdateDeadlines()
    {
        // lock, just in case
        lock (TimingLock)
        {
            ExecHardDeadline = Times.CurTime + (Config.ContextSwitchTime*20);
            ExecSoftDeadline = Times.CurTime + Config.ContextSwitchTime;
        }
    }

    public void DecPunish()
    {
        Interlocked.Decrement(ref Punishment);
    }

    public bool ShouldRun()
    {
        int mask1 = (int)VMState.Active;
        int mask2 = (int)(VMState.IOWait | VMState.Paused);
        Interlocked.And(ref mask1, State);
        Interlocked.And(ref mask2, State);
        return (mask1 > 0) && (mask2 == 0);
    }

    // used externally, forces a preempt yield.
    public void Pause()
    {
        
    }

    // used internally, forces a generic yield.
    public int Yield(lua_State L)
    {
        return lua_yield(L, 0);
    }

    public bool Resume(bool IgnoreDeadline=false)
    {
        UpdateDeadlines();
        int count = 0;
        int status = lua_resume(L, 0, 0, ref count);
        double end_time = Times.CurTime;
        if (end_time >= ExecHardDeadline && !IgnoreDeadline)
        {
            OnWatchdog?.Invoke();
            Console.WriteLine("Watchdog triggered");
            return true; // NOT INTO THE PIT, IT BURNS
        }
        bool dead = status != LUA_YIELD;
        if (dead && status != LUA_OK)
        {
            string err = lua_tostring(L, -1);
            OnError?.Invoke(err);
        }
        lua_pop(L, count);
        Punishment = (int)Math.Floor((end_time-ExecHardDeadline)/Config.ContextSwitchTime);
        return dead;
    }

    nint CurrentAlloc = 0;
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
        return Lock.TryEnter();
    }

    public void Exit()
    {
        Lock.Exit();
    }

    public void Start()
    {
        InitLuaState();
        Resume(true);
    }

    public void AddLibrary(Library lib)
    {
        Libs.Add(lib);
    }
}