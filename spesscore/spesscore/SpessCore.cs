using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using spesscore.IPC;
using spesscore.Terminal;
using spesscore.VM;
using spesscore.VM.Peripheral;
using WatsonWebsocket;

namespace spesscore;

class SpessCore
{
    public List<Computer> Computers = [];
    List<Object> PendingCalls = [];
    public List<string> Bwoinks = [];
    Dictionary<string, IPeripheral> Peripherals = [];
    public TerminalServer TServ;
    public byte[] MachineLua;
    public byte[] RareFox;
    public static SpessCore? Instance;
    public LuaExecutionManager Manager;
    Socket ipc;
    IPC.IPC coms;

    public IPC.IPC IPC => coms;

    long hookspeed = 0;

    byte[] TryRead(string path)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var str = assembly.GetManifestResourceStream(path);
        if (str == null)
        {
            // billions must die
            Console.Error.WriteLine("shit's fucked, can't find machine.lua");
            Environment.Exit(-1);
        }
        byte[] tmp = new byte[str.Length];
        str.Read(tmp);
        return tmp;
    }

    public SpessCore()
    {
        File.Delete(Config.IPCSocketPath);
        ipc = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        var endpoint = new UnixDomainSocketEndPoint(Config.IPCSocketPath);
        ipc.Bind(endpoint);
        ipc.Listen();
        TServ = new((ushort)Config.WebsocketPort); // if you set this higher than 0xFFFF i will kill you
        MachineLua = TryRead("spesscore.machine.lua");
        RareFox = TryRead("spesscore.rare_fox.six");
        TaskScheduler.UnobservedTaskException += (sender, args) => {
            string err = args.Exception.ToString();
            Console.Error.WriteLine("KILL: "+err);
            Bwoink(err);
        };
        Manager = new();
        // temp lua state
        /* lua_State L = Lua.luaL_newstate();
        Lua.luaL_openlibs(L);
        byte[] buf = TryRead("spesscore.bogomips.lua");
        Console.WriteLine(buf.Length);
        Lua.lua_pushcfunction(L, (c) =>
        {
            string err = Lua.lua_tostring(c, 1);
            Console.WriteLine(err);
            Bwoinks.Add(err);
            return 0;
        });
        Lua.luaL_loadbufferx(L, buf, (ulong)buf.Length, "=bogomips.lua", "t");
        if (Lua.lua_type(L, -1) != Lua.LUA_TFUNCTION)
        {
            var errstr = "Failed to load machine.lua: "+Lua.lua_tostring(L, -1);
            Bwoinks.Add(errstr);
            Console.WriteLine(errstr);
        }
        Lua.lua_call(L, 0, 0);
        if (0 == 0)
        {
            Lua.lua_getglobal(L, "_HOOKINT");
            hookspeed = Lua.luaL_checkinteger(L, -1);
            Console.WriteLine("Hook speed: "+hookspeed);
        } else
        {
            
        } */
    }

    public string NewID()
    {
        return Guid.NewGuid().ToString();
    }

    public void AddPeripheral<T>(T Peripheral) where T : IPeripheral
    {
        // generate ID
        string id = NewID();
        Peripheral.ID = id;
        Peripherals[id] = Peripheral;
    }

    public void DestroyPeripheral(string id)
    {
        if (Peripherals.TryGetValue(id, out IPeripheral? p))
        {
            p.Destroy();
        }
    }

    public T? GetPeripheral<T>(string id) where T : class, IPeripheral
    {
        if (Peripherals.TryGetValue(id, out IPeripheral? p))
        {
            if (p is T rtv)
            {
                return rtv;
            }
        }
        return null;
    }

    public Computer? CreateDemoComputer()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var str = assembly.GetManifestResourceStream("spesscore.bios.lua");
        if (str == null)
        {
            // fuck
            return null;
        }
        byte[] bios = new byte[str.Length];
        str.Read(bios);
        Computer comp = new();
        EEPROM eeprom = new(bios);
        AddPeripheral(eeprom);
        comp.AddPeripheral(eeprom);
        comp.eeprom = eeprom;
        TTY tty = new(0);
        AddPeripheral(tty);
        comp.AddPeripheral(tty);
        comp.SetLocalTTY(tty.ID);
        Computers.Add(comp);
        return comp;
    }

    async Task AwaitShutdown()
    {
        await Process.GetProcessById(Config.ParentPID).WaitForExitAsync();
        Console.WriteLine("Parent killed, shutting down!");
        Environment.Exit(0);
    }

    public void Start()
    {
        Manager.Start();
        while (true) {
            Socket sock = ipc.Accept();
            coms = new(sock);
            while (true) {
                if (coms.Next())
                {
                    coms.Flush();
                }
            }
        }
    }

    public void PushSignal(LuaSignal signal)
    {
        if (Peripherals.TryGetValue(signal.Sender, out IPeripheral val))
            Peripherals[signal.Sender].Computer?.PushSignal(signal);
    }

    public void Bwoink(string msg)
    {
        lock(Bwoinks) {
            Console.Error.WriteLine("BWOINK!: "+msg);
            Bwoinks.Add(msg);
        }
    }
}