using KeraLua;

namespace spesscore.VM.Components;

interface IComponent
{
    public delegate int ComponentCallback(Lua L);
    Dictionary<string, ComponentCallback> Callbacks { get; }
    string ComponentName { get; }
    string ID { get; set; }
    void Attach(Computer computer);
    void Detach(Computer computer);
}