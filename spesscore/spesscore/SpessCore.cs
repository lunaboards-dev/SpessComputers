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

    public static SpessCore? Instance;
    Socket ipc;

    public SpessCore()
    {
        ipc = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unknown);
        var endpoint = new UnixDomainSocketEndPoint(Config.IPCSocketPath);
        ipc.Bind(endpoint);
        ipc.Listen();
        TServ = new((ushort)Config.WebsocketPort); // if you set this higher than 0xFFFF i will kill you
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

    void CreateDemoComputer()
    {
        var assembly = Assembly.GetExecutingAssembly();
    }

    public void Start()
    {
        // await connection
        
        // close if we lose connection
    }
}