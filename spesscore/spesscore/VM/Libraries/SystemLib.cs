namespace spesscore.VM.Libraries;

using static spesscore.VM.Lua;
using static spesscore.VM.Helpers;

class SystemLib : Library
{
    Dictionary<string, lua_CFunction> funcs;
    Computer c;
    public SystemLib(Computer com) : base("system")
    {
        c = com;
        CritFuncDel = CriticalFunctionCallback;
        funcs = new()
        {
            
            {"preempt", IsPreempted},
            {"rare_fox", RareFoxDel},
            {"set_mem_baseline", SetMemoryBaseline},
            //{"set_thd", SetThread},
            {"thd_resume", ThdResume},
            {"int_yield", OnlyYield},
            {"is_iores", IsIoresume},
            {"critical", CriticalFunction}
        };
    }

    public override Dictionary<string, lua_CFunction> Functions => funcs;

    int ThdResume(lua_State L)
    {
        //Computer c = lua_ToObject<Computer>(L, 1);
        luaL_checktype(L, 1, LUA_TTHREAD);
        lua_State S = lua_tothread(L, 1);
        if (lua_status(S) != LUA_YIELD && lua_status(S) != LUA_OK)
        {
            luaL_error(L, "Attempt to resume dead thread.");
        }
        int args = lua_gettop(L);
        int nargs = 0;
        if (lua_checkstack(S, args-1) == 0)
        {
            return luaL_error(L, "Can't reserve space to resume coroutine");
        }
        lua_xmove(L, S, args-1);
        lock(c.VM.LuaLock) c.VM.TL = S;
        int status = lua_resume(S, L, args-1, ref nargs);
        if (status == LUA_OK || status == LUA_YIELD)
        {
            if (lua_checkstack(L, nargs+1) == 0)
            {
                lua_pop(S, nargs);
                lock(c.VM.LuaLock) c.VM.TL = L;
                return luaL_error(L, "Can't reserve space for coroutine yielded values");
            }
            lua_pushboolean(L, 1);
            lua_xmove(S, L, nargs++);
        } else
        {
            lua_pushboolean(L, 0);
            lua_xmove(S, L, 1);
            nargs = 2;
        }
        lock(c.VM.LuaLock) c.VM.TL = L;
        return nargs;
    }

    int OnlyYield(lua_State L)
    {
        return lua_yield(L, 0);
    }

    //static lua_CFunction IsPreDel = IsPreempted;
    int IsPreempted(lua_State L)
    {
        //Computer c = lua_ToObject<Computer>(L, 1);
        lua_pushboolean(L, c.VM.Paused ? 1 : 0);
        return 1;
    }

    int IsIoresume(lua_State L)
    {
        lua_pushboolean(L, 0);
        //lua_pushboolean(L, c.iores ? 1 : 0);
        return 1;
    }

    static lua_CFunction RareFoxDel = RareFox;
    static int RareFox(lua_State L)
    {
        lua_pushbytebuffer(L, SpessCore.Instance.RareFox);
        return 1;
    }

    //static lua_CFunction SetMemBase = SetMemoryBaseline;
    int SetMemoryBaseline(lua_State L)
    {
        //Computer c = lua_ToObject<Computer>(L, 1);
        c.VM.CurrentAlloc = 0;
        return 0;
    } // DO NOT EXPOSE THIS

    lua_CFunction CritFuncDel;
    int CriticalFunctionCallback(lua_State L)
    {
        int n_args = lua_gettop(L);
        lua_pushvalue(L, lua_upvalueindex(1));
        lua_rotate(L, 1, 1);
        c.VM.EnterCritical();
        int status = lua_pcall(L, n_args, LUA_MULTRET, 0);
        c.VM.ExitCritical();
        c.VM.Pause(); // you should probably yield at the earliest possible moment
        if (status != LUA_OK)
        {
            string err = lua_tostring(L, -1);
            lua_pushstring(L, "error in critical region: "+err);
            return lua_error(L);
        }
        return lua_gettop(L);
    }

    int CriticalFunction(lua_State L)
    {
        luaL_checktype(L, 1, LUA_TFUNCTION);
        lua_pushvalue(L, 1);
        lua_pushcclosure(L, CritFuncDel, 1);
        return 1;
    }
}