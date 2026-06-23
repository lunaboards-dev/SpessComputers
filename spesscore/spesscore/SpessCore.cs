using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using spesscore.Terminal;
using spesscore.VM;
using spesscore.VM.Peripheral;
using WatsonWebsocket;

namespace spesscore;

class SpessCore
{
    List<Computer> Computers = [];
    List<Object> PendingCalls = [];
    Dictionary<string, IPeripheral> Peripherals = [];
    public TerminalServer TServ;
    public byte[] MachineLua;

    public static SpessCore? Instance;
    Socket ipc;

    public SpessCore()
    {
        ipc = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        var endpoint = new UnixDomainSocketEndPoint(Config.IPCSocketPath);
        ipc.Bind(endpoint);
        ipc.Listen();
        TServ = new((ushort)Config.WebsocketPort); // if you set this higher than 0xFFFF i will kill you
        var assembly = Assembly.GetExecutingAssembly();
        using var str = assembly.GetManifestResourceStream("spesscore.machine.lua");
        if (str == null)
        {
            // billions must die
            Console.Error.WriteLine("shit's fucked, can't find machine.lua");
            Environment.Exit(-1);
        }
        MachineLua = new byte[str.Length];
        str.Read(MachineLua);
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
        TTY tty = new();
        AddPeripheral(tty);
        comp.AddPeripheral(tty);
        comp.SetLocalTTY(tty.ID);
        return comp;
    }

    public void Start()
    {
        // await connection
        
        // close if we lose connection
    }
}