namespace spesscore.VM.Libraries;

using static spesscore.VM.Lua;
using static spesscore.VM.Helpers;
using spesscore.VM.Peripheral;

class ComputerLib : Library
{
    Computer c;
    Dictionary<string, lua_CFunction> funcs;
    public ComputerLib(Computer comp) : base("computer")
    {
        c = comp;
        funcs = new()
        {
            {"peripheral", PeripheralById},
            {"peripherals", PeripheralList},
            {"eeprom", GetEEPROM},
            {"tty", GetTTY},
            {"disk", GetDisk},
            {"mem_total", GetMemory},
            {"mem_used", GetUsedMemory},
            {"preempt", IsPreempted},
            {"rare_fox", RareFoxDel},
            {"set_mem_baseline", SetMemoryBaseline},
            //{"set_thd", SetThread},
            {"pull_signal", PullSignal},
            {"thd_resume", ThdResume},
            {"int_yield", OnlyYield}
        };
    }

    public override Dictionary<string, lua_CFunction> Functions => funcs;

    //static lua_CFunction PerByIdDel = PeripheralById;
    int PeripheralById(lua_State L)
    {
        //Computer? c = lua_ToObject<Computer>(L, 1);
        string? id = luaL_checkstring(L, 1);
        var perf = c.GetPeripheral(id);
        if (perf == null)
            return 0;
        c.PushPeripheral(L, perf);
        return 1;
    }

    static lua_CFunction PIterDel = PeripheralIter;
    static int PeripheralIter(lua_State L)
    {
        //Lua L = Lua.FromIntPtr(ptr);
        List<IPeripheral>? pl = lua_ToObject<List<IPeripheral>>(L, lua_upvalueindex(1));//L.ToObject<List<IPeripheral>>(Lua.UpValueIndex(1), false);
        long index = lua_tointeger(L, lua_upvalueindex(2));//L.ToInteger(Lua.UpValueIndex(2));
        if (index == pl?.Count)
        {
            return 0;
        }
        var p = pl?[(int)index++];
        lua_pushinteger(L, index);
        lua_replace(L, lua_upvalueindex(2));//L.Replace(Lua.UpValueIndex(2));
        lua_pushstring(L, p.ID);//L.PushString(p.ID);
        lua_pushstring(L, p.PeripheralName);//L.PushString(p.PeripheralName);
        return 2;
    }

    //static lua_CFunction PerListDel = PeripheralList;
    int PeripheralList(lua_State L)
    {
        //Computer c = lua_ToObject<Computer>(L, 1);//L.ToObject<Computer>(1, false);
        if (lua_isnil(L, 1))
        {
            lua_PushObjectManaged(L, c.Peripherals);
            lua_pushinteger(L, 0);
            lua_pushcclosure(L, PIterDel, 2);
            return 1;
        } else
        {
            string type = luaL_checkstring(L, 1); //L.CheckString(2);
            List<IPeripheral> pl = c.Peripherals.Where((p) => p.PeripheralName.StartsWith(type)).ToList();
            lua_PushObjectManaged(L, pl);//L.PushObject(pl);
            lua_pushinteger(L, 0);//L.PushInteger(0);
            lua_pushcclosure(L, PIterDel, 2);//L.PushCClosure(PIterDel, 2);
            return 1;
        }
    }

    //static lua_CFunction GetROMDel = GetEEPROM;
    int GetEEPROM(lua_State L)
    {
        //Computer c = lua_ToObject<Computer>(L, 1);//L.ToObject<Computer>(1, false);
        if (c.eeprom == null) return 0;
        c.PushPeripheral(L, c.eeprom);
        return 1;
    }

    //static lua_CFunction GetTTYDel = GetTTY;
    int GetTTY(lua_State L)
    {
        //Computer c = lua_ToObject<Computer>(L, 1);//Computer c = L.ToObject<Computer>(1, false);
        //DumpStack(L);
        if (c == null)
        {
            Console.WriteLine("computer is null");
            return 0;
        }
        if (c.LocalTTY == null) return 0;
        c.PushPeripheral(L, c.LocalTTY);
        return 1;
    }

    //static lua_CFunction GetDskDel = GetDisk;
    int GetDisk(lua_State L)
    {
        //Computer c = lua_ToObject<Computer>(L, 1);//Computer c = L.ToObject<Computer>(1, false);
        if (c.Disk == null) return 0;
        c.PushPeripheral(L, c.Disk);
        return 1;
    }

    //static lua_CFunction RamTotalDel = GetMemory;
    int GetMemory(lua_State L)
    {
        //Computer c = lua_ToObject<Computer>(L, 1);
        lua_pushinteger(L, c.max_memory);
        return 1;
    }

    //static lua_CFunction RamUseDel = GetUsedMemory;
    int GetUsedMemory(lua_State L)
    {
        //Computer c = lua_ToObject<Computer>(L, 1);
        lua_pushinteger(L, c.currently_allocated);
        return 1;
    }

    //static lua_CFunction IsPreDel = IsPreempted;
    int IsPreempted(lua_State L)
    {
        //Computer c = lua_ToObject<Computer>(L, 1);
        lua_pushboolean(L, c.paused ? 1 : 0);
        return 1;
    }

    //static lua_CFunction SetThdDel = SetThread;
    int SetThread(lua_State L)
    {
        //Computer c = lua_ToObject<Computer>(L, 1);
        int main = lua_pushthread(L);
        lock(c.PLL) c.PL = lua_tothread(L, -1);
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
        c.currently_allocated = 0;
        return 0;
    } // DO NOT EXPOSE THIS

    //static lua_CFunction PullSigDel = PullSignal;
    int PullSignal(lua_State L)
    {
        //Computer c = lua_ToObject<Computer>(L, 1);
        if (lua_type(L, 1) == LUA_TNUMBER)
        {
            double dl = lua_tonumber(L, 1);
            c.Deadline = Times.CurTime + dl;
        }
        int res = 0;
        /* if (c.SignalCount > 0)
        {
            var sig = c.events.Next();
            var val = sig?.Push(L);
            if (val != null)
                res = val.Value;
        } */
        Console.WriteLine("Yielded");
        return lua_yield(L, res); // i hope this doesn't explode
    }

    //static lua_CFunction ThdResDel = ThdResume;
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
        Console.WriteLine($"Resuming thread {S}");
        int nargs = 0;
        lock(c.PLL) c.PL = S;
        lua_resume(S, L, args-1, ref nargs);
        lock(c.PLL) c.PL = L;
        return nargs;
    }

    int OnlyYield(lua_State L)
    {
        return lua_yield(L, 0);
    }
}