
using KeraLua;

namespace spesscore.VM.Components;

class TTY : IComponent
{
    public Dictionary<string, IComponent.ComponentCallback> Callbacks => throw new NotImplementedException();

    public string ComponentName => "tty";

    public string ID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public void Attach(Computer computer)
    {
        throw new NotImplementedException();
    }

    public void Detach(Computer computer)
    {
        throw new NotImplementedException();
    }

    int Write(Lua L)
    {
        string str = L.CheckString(1);
        return 0;
    }
}