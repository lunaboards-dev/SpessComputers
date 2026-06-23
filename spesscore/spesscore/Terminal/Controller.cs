using System.Text;
using Newtonsoft.Json;
using spesscore.VM;

namespace spesscore.Terminal;

class Controller
{
    public Guid gid;

    public void Process(byte[] data)
    {
        string jtext = Encoding.UTF8.GetString(data);
        var basic_command = new {command = ""};
        var cmd = JsonConvert.DeserializeAnonymousType(jtext, basic_command);
        if (cmd == null) return;
        if (cmd.command == "new_computer")
        {
            Computer? c = SpessCore.Instance?.CreateDemoComputer();
            if (c == null) return;
            string rtext = JsonConvert.SerializeObject(new {command="new_computer", id=c.GetPeripherals("tty").First().ID});
            SpessCore.Instance?.TServ.Server.SendAsync(gid, Encoding.UTF8.GetBytes(rtext));
        }
    }
}