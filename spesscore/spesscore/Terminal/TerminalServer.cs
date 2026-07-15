using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;
using spesscore.VM.Peripheral;
using WatsonWebsocket;

namespace spesscore.Terminal;

class TerminalServer
{
    public WatsonWsServer Server;
    HashSet<Guid> Pending = [];
    Dictionary<string, TerminalListener> Listeners = [];
    Dictionary<Guid, TerminalListener> GuidLookup = [];
    Controller? debug;

    public TerminalServer(ushort port)
    {
        Server = new("192.168.1.99", port, false);
        Server.ClientConnected += WsConnected;
        Server.MessageReceived += WsMessage;
        Server.ClientDisconnected += WsDisconnect;
        Server.Start();
        Console.WriteLine($"WS server started on {port}");
    }

    private void WsDisconnect(object? sender, DisconnectionEventArgs e)
    {
        var cid = e.Client.Guid;
        Pending.Remove(cid);
        if (GuidLookup.TryGetValue(cid, out TerminalListener? list))
        {
            list.Ctx.Remove(cid);
            GuidLookup.Remove(cid);
        }
    }

    public TerminalListener NewListener(string ID)
    {
        TerminalListener list = new(ID);
        Listeners.Add(ID, list);
        return list;
    }

    /*
        Extremely simple protocol.
        byte 0 - msg type
        // the rest of the fucking message //
        msg types:
        0x00 - connect
        0x01 - stdin
        0x02 - signaling (json)
    */
    void WsMessage(object? sender, MessageReceivedEventArgs args)
    {
        var cid = args.Client.Guid;
        if (debug?.gid == cid)
        {
            if (args.Data.Array != null) debug.Process(args.Data.Array);
            return;
        }
        if (Pending.Contains(cid))
        {
            if (args.Data[0] == 0 && args.Data.Array != null) // i have no fucking clue why it would ever be null
            {
                string uuid = Encoding.ASCII.GetString(args.Data.Array, 1, args.Data.Count-1);
                if (Listeners.TryGetValue(uuid, out TerminalListener? Listener))
                {
                    Listener.Ctx.Add(cid);
                    GuidLookup.Add(cid, Listener);
                    Pending.Remove(cid);
                    Console.WriteLine($"{cid} connected to tty {uuid}.");
                    Listener.Write(Encoding.ASCII.GetBytes("(DEBUG) Connected!"));
                    return; // LIVE
                }
            }
        } else if (GuidLookup.TryGetValue(cid, out TerminalListener? Listener))
        {
            if (args.Data.Array != null) {
                byte[] buffer = new byte[args.Data.Count-1];
                Array.Copy(args.Data.Array, 1, buffer, 0, buffer.Length);
                Console.WriteLine($"{cid} executing command {args.Data[0]}.");
                if (args.Data[0] == 1)
                {
                    Listener.Recieved(buffer); // yippie
                    return;
                } else if (args.Data[0] == 2)
                {
                    string jstr = Encoding.UTF8.GetString(buffer);
                    var _cmd = new {command=""};
                    var cmd = JsonConvert.DeserializeAnonymousType(jstr, _cmd);
                    if (cmd == null)  {
                        Console.WriteLine($"Failed to parse JSON from {cid}");
                        return; // we should probably kill the man who sent this
                    }
                    Console.WriteLine($"{cid} (via {Listener.ID}) -> command: {cmd.command}");
                    if (cmd.command == "power")
                    {
                        var _pwrcmd = new {command="", hard=false};
                        var pwr = JsonConvert.DeserializeAnonymousType(jstr, _pwrcmd);
                        var term = SpessCore.Instance?.GetPeripheral<TTY>(Listener.ID);
                        if (term == null || term.Computer == null || term.Computer.LocalTerminal == null)
                        {
                            Console.WriteLine($"Something is null! {term} {term?.Computer} {term?.Computer?.LocalTerminal}");
                        }
                        if (term?.Computer?.LocalTerminal != term)
                        {
                            // mr electric, send this man to the penis explosion chamber
                            Console.WriteLine($"{cid} sent power from non-local terminal!");
                            return;
                        }
#pragma warning disable CS8629 // Nullable value type may be null.
                        term?.Computer?.TogglePower(pwr?.hard != null && (bool)pwr?.hard); // who fucking cares if it's null
#pragma warning restore CS8629 // Nullable value type may be null.
                        return;
                    } else if (cmd.command == "resume")
                    {
                        var term = SpessCore.Instance?.GetPeripheral<TTY>(Listener.ID);
                        if (term == null || term.Computer == null || term.Computer.LocalTerminal == null)
                        {
                            Console.WriteLine($"Something is null! {term} {term?.Computer} {term?.Computer?.LocalTerminal}");
                        }
                        if (term?.Computer?.LocalTerminal != term)
                        {
                            // mr electric, send this man to the penis explosion chamber
                            Console.WriteLine($"{cid} sent power from non-local terminal!");
                            return;
                        }
                        term?.Computer?.TryResume();
                    }
                    return;
                }
            }
        }
        Server.DisconnectClient(cid); // destroy the child
    }

    void WsConnected(object? sender, ConnectionEventArgs args)
    {
        Console.WriteLine($"client connect {args.Client} -> {args.HttpRequest.Url}");
        if (args.HttpRequest.Url?.AbsolutePath == "/ctl")
        {
            if (!Config.DebugEnableControlWS)
            {
                Server.DisconnectClient(args.Client.Guid);
            } else
            {
                debug = new Controller()
                {
                    gid = args.Client.Guid
                };
            }
        } else if (args.HttpRequest.Url?.AbsolutePath == "/tty")
        {
            Pending.Add(args.Client.Guid);
        } else
        {
            Server.DisconnectClient(args.Client.Guid);
        }
    }

    internal void Remove(string id)
    {
        Listeners.Remove(id);
    }
}