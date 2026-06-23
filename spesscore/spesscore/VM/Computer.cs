using System.Runtime.InteropServices;
using KeraLua;
using spesscore.VM.Peripheral;

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

    Lua L;

    static void DumpStack(Lua l)
    {
        Console.WriteLine("!! STACK DUMP START");
        int size = l.GetTop();
        for (int i=0;i<size;++i)
        {
            string s = l.ToString(i+1);
            string t = l.TypeName(i+1);
            Console.WriteLine($"({t})\t{s}");
        }
        Console.WriteLine("!! STACK DUMP END");
    }

    unsafe nint Allocator(nint ud, nint ptr, nuint osize, nuint nsize)
    {
        nint delta = ((int)nsize)-((int)osize);
        if (delta+currently_allocated > max_memory)
        {
            Console.WriteLine("OOM");
            return 0; // wrong, chlorine trifluoride
        }
        void* p = NativeMemory.Realloc((void*)ptr, nsize);
        currently_allocated+=(int)delta;
        return (nint)p;
    }

    delegate int CSFunc(Lua L);

    static int Recaller(IntPtr ctx)
    {
        Lua L = Lua.FromIntPtr(ctx);
        CSFunc f = L.ToObject<CSFunc>(Lua.UpValueIndex(1));
        return f(L);
    }

    static void PushCSFunc(Lua L, CSFunc f)
    {
        L.PushObject(f);
        L.PushCClosure(Recaller, 1);
    }

    static int PeripheralById(Lua L)
    {
        Computer c = L.ToObject<Computer>(1);
        string id = L.CheckString(2);
        var perf = c.GetPeripheral(id);
        if (perf == null)
            return 0;
        c.PushPeripheral(L, perf);
        return 1;
    }

    static int PeripheralIter(IntPtr ptr)
    {
        Lua L = Lua.FromIntPtr(ptr);
        List<IPeripheral> pl = L.ToObject<List<IPeripheral>>(Lua.UpValueIndex(1));
        long index = L.ToInteger(Lua.UpValueIndex(2));
        if (index == pl.Count)
        {
            return 0;
        }
        var p = pl[(int)index++];
        L.PushInteger(index);
        L.Replace(Lua.UpValueIndex(2));
        L.PushString(p.ID);
        L.PushString(p.PeripheralName);
        return 2;
    }

    static int PeripheralList(Lua L)
    {
        Computer c = L.ToObject<Computer>(1);
        if (L.Type(2) == LuaType.Nil)
        {
            L.PushObject(c.Peripherals);
            L.PushInteger(0);
            L.PushCClosure(PeripheralIter, 2);
            return 1;
        } else
        {
            string type = L.CheckString(2);
            List<IPeripheral> pl = c.Peripherals.Where((p) => p.PeripheralName.StartsWith(type)).ToList();
            L.PushObject(pl);
            L.PushInteger(0);
            L.PushCClosure(PeripheralIter, 2);
            return 1;
        }
    }

    static int GetEEPROM(Lua L)
    {
        Computer c = L.ToObject<Computer>(1);
        if (c.eeprom == null) return 0;
        c.PushPeripheral(L, c.eeprom);
        return 1;
    }

    static int GetTTY(Lua L)
    {
        Computer c = L.ToObject<Computer>(1);
        if (c.LocalTTY == null) return 0;
        c.PushPeripheral(L, c.LocalTTY);
        return 1;
    }

    static int GetDisk(Lua L)
    {
        Computer c = L.ToObject<Computer>(1);
        if (c.Disk == null) return 0;
        c.PushPeripheral(L, c.Disk);
        return 1;
    }

    void InitLuaState()
    {
        L = new Lua(Allocator, 0);
        L.OpenLibs();
        
        // push functions
        L.PushString("peripheral_by_id");
        PushCSFunc(L, PeripheralById);
        L.SetTable(-3);

        L.PushString("peripherals");
        PushCSFunc(L, PeripheralList);
        L.SetTable(-3);

        L.PushString("eeprom");
        PushCSFunc(L, GetEEPROM);
        L.SetTable(-3);

        L.PushString("tty");
        PushCSFunc(L, GetTTY);
        L.SetTable(-3);

        L.PushString("disk");
        PushCSFunc(L, GetDisk);
        L.SetTable(-3);

        L.SetGlobal("_computer");
    }

    int PeripheralCall(nint ptr)
    {
        Lua L = Lua.FromIntPtr(ptr);
        Computer c = L.ToObject<Computer>(Lua.UpValueIndex(1));
        IPeripheral p = L.ToObject<IPeripheral>(Lua.UpValueIndex(2));
        IPeripheral.PeripheralCallback cb = L.ToObject<IPeripheral.PeripheralCallback>(Lua.UpValueIndex(3));
        if (p.Computer != c) return 0;
        return cb(L);
    }

    void PushPeripheral<T>(Lua l, T p) where T : IPeripheral
    {
        l.PushObject(p);
        l.NewTable();
        l.PushString("__tostring");
        /* l.PushCFunction((ptr) =>
        {
            Lua ctx = Lua.FromIntPtr(ptr);
            ctx.PushString(p.GetType().FullName);
            return 1;
        });
        l.SetTable(-3);*/
        l.PushString("__index");
        l.NewTable();
        foreach (var method in p.Callbacks)
        {
            l.PushString(method.Key);
            l.PushObject(this);
            l.PushObject(p);
            l.PushObject(method.Value);
            l.PushCClosure(PeripheralCall, 3);
            l.SetTable(-3);
        }
        l.SetTable(-3);
        l.SetMetaTable(-2);
    }

    public void MemoryResize(int newsize)
    {
        if (currently_allocated > newsize)
        {
            L.Error("Out of memory");
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
    void Stop()
    {
        Pause();
        active = false;
    }

    async Task Start()
    {
        try {
            InitLuaState(); // oh my fucking god bruh
            active = true;
            L.PushCFunction((ptr) =>
            {
                Lua ctx = Lua.FromIntPtr(ptr);
                string str = ctx.ToString(1);
                ctx.Traceback(ctx, str, 1);
                string traceback = ctx.ToString(-1);
                SpessCore.Instance?.Bwoinks.Add(traceback);
                Console.WriteLine("Uncaught error: "+traceback);
                return 0;
            });
            L.LoadBuffer(SpessCore.Instance?.MachineLua, "machine.lua");
            L.PCall(0, 0, -2);
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