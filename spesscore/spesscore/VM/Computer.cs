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

    unsafe nint Allocator(nint ud, nint ptr, nuint osize, nuint nsize)
    {
        nint delta = ((int)nsize)-((int)osize);
        if (delta+currently_allocated > max_memory)
        {
            return 0; // wrong, chlorine trifluoride
        }
        void* p = NativeMemory.Realloc((void*)ptr, nsize);
        currently_allocated+=(int)delta;
        return (nint)p;
    }

    void InitLuaState()
    {
        L = new Lua(Allocator, 0);
        L.OpenLibs();
        L.NewTable();
        // push functions
        L.PushString("peripheral_by_id");
        L.PushCFunction((ptr) =>
        {
            Lua ctx = Lua.FromIntPtr(ptr);
            string id = ctx.CheckString(1);
            var perf = GetPeripheral(id);
            if (perf == null)
                return 0;
            PushPeripheral(ctx, perf);
            return 1;
        });
        L.SetTable(-3);

        L.PushString("peripherals");
        L.PushCFunction((ptr) =>
        {
            List<IPeripheral> list;
            if (L.IsNil(1))
            {
                list = Peripherals;
            } else
            {
                string ct = L.CheckString(1);
                list = Peripherals.Where(p => p.PeripheralName.StartsWith(ct)).ToList();
            }
            int idx = 0;
            L.PushCFunction((ptr) =>
            {
                Lua ctx = Lua.FromIntPtr(ptr);
                if (idx == list.Count) return 0;
                var p = list[idx];
                ctx.PushString(p.ID);
                ctx.PushString(p.PeripheralName);
                return 2;
            });
            return 1;
        });
        L.SetTable(-3);

        L.PushString("eeprom");
        L.PushCFunction((ptr) =>
        {
            Lua ctx = Lua.FromIntPtr(ptr);
            if (eeprom == null) return 0;
            PushPeripheral(ctx, eeprom);
            return 1;
        });
        L.SetTable(-3);

        L.PushString("tty");
        L.PushCFunction((ptr) =>
        {
            Lua ctx = Lua.FromIntPtr(ptr);
            if (LocalTTY == null) return 0;
            PushPeripheral(ctx, LocalTTY);
            return 1;
        });
        L.SetTable(-3);

        L.PushString("disk");
        L.PushCFunction((ptr) =>
        {
            Lua ctx = Lua.FromIntPtr(ptr);
            if (Disk == null) return 0;
            PushPeripheral(ctx, Disk);
            return 1;
        });
        L.SetTable(-3);

        L.SetGlobal("computer");
    }

    void PushPeripheral<T>(Lua l, T p) where T : class, IPeripheral
    {
        l.PushObject(p);
        l.NewTable();
        l.PushString("__index");
        l.NewTable();
        foreach (var method in p.Callbacks)
        {
            l.PushString(method.Key);
            l.PushCFunction((ptr) =>
            {
                Lua ctx = Lua.FromIntPtr(ptr);
                T px = ctx.ToObject<T>(1);
                if (px == null)
                {
                    return L.Error("internal error");
                }
                if (px.Computer != this)
                {
                    return L.Error("Peripheral not connected.");
                }
                return method.Value(ctx);
            });
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