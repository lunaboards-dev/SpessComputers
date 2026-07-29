// contains all core VM stuff
using spesscore.VM.Libraries;
using static spesscore.VM.Lua;
using static spesscore.VM.Helpers;

namespace spesscore.VM;

class VMCore
{
    lua_State L;
    lua_State TL;

    Lock LuaLock = new(); // required for accessing either L or TL.

    double ExecSoftDeadline;
    double ExecHardDeadline;
    double ResumeDeadline;
    int Punishment = 0;
    Lock TimingLock = new();

    int State = 0;

    void UpdateDeadlines()
    {
        // lock, just in case
        lock (TimingLock)
        {
            ExecHardDeadline = Times.CurTime + (Config.ContextSwitchTime*20);
            ExecSoftDeadline = Times.CurTime + Config.ContextSwitchTime;
        }
    }

    void DecPunish()
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
    void Pause()
    {
        
    }

    // used internally, forces a generic yield.
    int Yield(lua_State L)
    {
        return lua_yield(L, 0);
    }

    bool Resume()
    {
        return false;
    }

    void Start()
    {
        
    }

    void AddLibrary(Library lib)
    {
        lib.Push(L);
    }
}