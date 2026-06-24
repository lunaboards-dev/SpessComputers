using System.Runtime.InteropServices;
using spesscore.VM.Peripheral;

using static spesscore.VM.Lua;
using static spesscore.VM.Helpers;
using System.Text;

namespace spesscore.VM;

class Computer
{
    int max_memory = 1024*1024;
    int cpu_speed;
    int currently_allocated;
    List<IPeripheral> Peripherals = [];
    RingBuffer<LuaSignal> events = new((uint)Config.EventBufferSize);
    TTY? LocalTTY;
    public TTY? LocalTerminal => LocalTTY;
    public EEPROM? eeprom;
    ManagedDisk? Disk;

    lua_Alloc MemAlloc;

    lua_State L;

    // cache delegates here
    public Computer()
    {
        MemAlloc = Allocator;
    }

    static void DumpStack(lua_State L)
    {
        Console.WriteLine("!! STACK DUMP START");
        int size = lua_gettop(L);
        for (int i=0;i<size;++i)
        {
            string? s = lua_tostring(L, 1);//l.ToString(i+1);
            string? t = luaL_typename(L, 1);//l.TypeName(i+1);
            Console.WriteLine($"({t})\t{s}");
        }
        Console.WriteLine("!! STACK DUMP END");
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

    static lua_CFunction PerByIdDel = PeripheralById;
    static int PeripheralById(lua_State L)
    {
        Computer? c = lua_ToObject<Computer>(L, 1);
        string? id = luaL_checkstring(L, 2);
        var perf = c?.GetPeripheral(id);
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

    static lua_CFunction PerListDel = PeripheralList;
    static int PeripheralList(lua_State L)
    {
        Computer c = lua_ToObject<Computer>(L, 1);//L.ToObject<Computer>(1, false);
        if (lua_isnil(L, 2) != 0)
        {
            lua_PushObjectManaged(L, c.Peripherals);
            lua_pushinteger(L, 0);
            lua_pushcclosure(L, PIterDel, 2);
            return 1;
        } else
        {
            string type = luaL_checkstring(L, 2); //L.CheckString(2);
            List<IPeripheral> pl = c.Peripherals.Where((p) => p.PeripheralName.StartsWith(type)).ToList();
            lua_PushObjectManaged(L, pl);//L.PushObject(pl);
            lua_pushinteger(L, 0);//L.PushInteger(0);
            lua_pushcclosure(L, PIterDel, 2);//L.PushCClosure(PIterDel, 2);
            return 1;
        }
    }

    static lua_CFunction GetROMDel = GetEEPROM;
    static int GetEEPROM(lua_State L)
    {
        Computer c = lua_ToObject<Computer>(L, 1);//L.ToObject<Computer>(1, false);
        if (c.eeprom == null) return 0;
        c.PushPeripheral(L, c.eeprom);
        return 1;
    }

    static lua_CFunction GetTTYDel = GetTTY;
    static int GetTTY(lua_State L)
    {
        Computer c = lua_ToObject<Computer>(L, 1);//Computer c = L.ToObject<Computer>(1, false);
        DumpStack(L);
        if (c.LocalTTY == null) return 0;
        c.PushPeripheral(L, c.LocalTTY);
        return 1;
    }

    static lua_CFunction GetDskDel = GetDisk;
    static int GetDisk(lua_State L)
    {
        Computer c = lua_ToObject<Computer>(L, 1);//Computer c = L.ToObject<Computer>(1, false);
        if (c.Disk == null) return 0;
        c.PushPeripheral(L, c.Disk);
        return 1;
    }

    void InitLuaState()
    {
        Console.WriteLine("LUA OPEN");
        L = lua_newstate(MemAlloc, 0);//new Lua(Allocator, 0);
        Console.WriteLine("LUA LIB");
        luaL_openlibs(L);
        
        lua_PushObjectManaged(L, this);//L.PushObject(this);
        lua_newtable(L);//L.NewTable();
        lua_pushstring(L, "__index");//L.PushString("__index");
        lua_newtable(L);//L.NewTable();
        Console.WriteLine("LUA pid");
        // push functions
        lua_pushstring(L, "peripheral_by_id");//L.PushString("peripheral_by_id");
        lua_pushcfunction(L, PerByIdDel);//PushCSFunc(L, PeripheralById);
        lua_settable(L, -3);//L.SetTable(-3);

        Console.WriteLine("LUA plist");
        lua_pushstring(L, "peripherals");//L.PushString("peripherals");
        lua_pushcfunction(L, PerListDel);//PushCSFunc(L, PeripheralList);
        lua_settable(L, -3);//L.SetTable(-3);

        Console.WriteLine("LUA eeprom");
        lua_pushstring(L, "eeprom");
        lua_pushcfunction(L, GetROMDel);
        lua_settable(L, -3);

        Console.WriteLine("LUA tty");
        lua_pushstring(L, "tty");
        lua_pushcfunction(L, GetTTYDel);
        lua_settable(L, -3);

        Console.WriteLine("LUA disk");
        lua_pushstring(L, "disk");
        lua_pushcfunction(L, GetDskDel);
        lua_settable(L, -3);
        
        lua_settable(L, -3);

        lua_pushstring(L, "__gc");
        lua_pushcfunction(L, ReleaseObjectDelegate);
        lua_settable(L, -3);

        lua_setmetatable(L, -2);

        Console.WriteLine("LUA GLOBAL");
        lua_setglobal(L, "_computer");
        Console.WriteLine("LUA end");
    }

    static lua_CFunction PerCallDel = PeripheralCall;
    static int PeripheralCall(lua_State L)
    {
        Computer c = lua_ToObject<Computer>(L, lua_upvalueindex(1));
        IPeripheral p = lua_ToObject<IPeripheral>(L, lua_upvalueindex(2));
        //IPeripheral.PeripheralCallback cb = lua_ToObject<IPeripheral.PeripheralCallback>(L, lua_upvalueindex(3));
        string method = lua_tostring(L, lua_upvalueindex(3));
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

    void PushPeripheral<T>(lua_State l, T p) where T : IPeripheral
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
        //ctx.Traceback(ctx, str, 1);
        string traceback = luaL_checkstring(L, -1);//ctx.ToString(-1);
        SpessCore.Instance?.Bwoinks.Add(traceback);
        Console.WriteLine("Uncaught error: "+traceback);
        return 0;
    }

    async Task Start()
    {
        try {
            InitLuaState(); // oh my fucking god bruh
            active = true;
            lua_pushcfunction(L, BwoinkDel);
            //luaL_loadstring(L, Encoding.UTF8.GetString(SpessCore.Instance?.MachineLua));
            luaL_loadbuffer(L, SpessCore.Instance?.MachineLua, (uint)SpessCore.Instance?.MachineLua.Length, "=machine.lua");
            //L.LoadBuffer(SpessCore.Instance?.MachineLua, "=machine.lua");
            lua_pcall(L, 0, 0, -2);//L.PCall(0, 0, -2);
        } catch (Exception e)
        {
            Console.Write(e);
        }
    }

    void Pause()
    {
        
    }

    public void TogglePower(bool hard)
    {
        if (active) Stop();
        else Start();
    }
}