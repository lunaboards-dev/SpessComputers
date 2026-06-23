using KeraLua;

namespace spesscore.VM.Peripheral;

interface IPeripheral
{
    public delegate int PeripheralCallback(Lua L);
    Dictionary<string, PeripheralCallback> Callbacks { get; }
    string PeripheralName { get; }
    string ID { get; set; }
    Computer? Computer { get; }
    void Attach(Computer computer);
    void Detach(Computer computer);
    void Destroy();
}