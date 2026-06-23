using System.Text;
using WatsonWebsocket;

namespace spesscore.Terminal;

class TerminalServer
{
    public WatsonWsServer Server;
    HashSet<Guid> Pending = [];
    Dictionary<string, TerminalListener> Listeners = [];
    Dictionary<Guid, TerminalListener> GuidLookup = [];

    public TerminalServer(ushort port)
    {
        Server = new("127.0.0.1", port, false);
        Server.ClientConnected += WsConnected;
        Server.MessageReceived += WsMessage;
        Server.ClientDisconnected += WsDisconnect;
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
                    return; // LIVE
                }
            }
        } else if (GuidLookup.TryGetValue(cid, out TerminalListener? Listener))
        {
            if (args.Data.Array != null) {
                if (args.Data[0] == 1)
                {
                    byte[] buffer = new byte[args.Data.Count-1];
                    Array.Copy(args.Data.Array, 1, buffer, 0, buffer.Length);
                    Listener.Recieved(buffer); // yippie
                    return;
                } else if (args.Data[0] == 2)
                {
                    // we're not handling signaling yet, but we definitely should
                    return;
                }
            }
        }
        Server.DisconnectClient(cid); // destroy the child
    }

    void WsConnected(object? sender, ConnectionEventArgs args)
    {
        if (args.HttpRequest.Url?.AbsolutePath == "/ctl")
        {
            if (!Config.DebugEnableControlWS)
            {
                Server.DisconnectClient(args.Client.Guid);
            } else
            {
                // debugging lol
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