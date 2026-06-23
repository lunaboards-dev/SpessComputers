using System.Diagnostics;
using System.Net.WebSockets;
using WatsonWebsocket;

namespace spesscore.Terminal;

class TerminalListener(string id)
{
    // you can shut up now
#pragma warning disable CS8602 // Dereference of a possibly null reference.
    TerminalServer srv = SpessCore.Instance.TServ;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    public string ID => id;
    //public Process Persister;
    public List<Guid> Ctx = [];
    public event Action<byte[]>? OnInput;

    internal void Recieved(byte[] data)
    {
        if (OnInput != null)
        {
            OnInput(data);
        }
    }

    public void Write(byte[] data)
    {
        foreach (var id in Ctx)
        {
            srv.Server.SendAsync(id, data); // we will never have instance be null at this point lol
        }
    }

    public void Kill()
    {
        foreach (var id in Ctx)
        {
            srv.Server.DisconnectClient(id);
        }
        srv.Remove(ID);
    }
}