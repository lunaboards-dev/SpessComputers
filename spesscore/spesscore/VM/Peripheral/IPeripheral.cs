namespace spesscore.VM.Peripheral;

using PeripheralCallback = Lua.lua_CFunction;

interface IPeripheral
{
    //public delegate int PeripheralCallback(lua_State L);
    Dictionary<string, Lua.lua_CFunction> Callbacks { get; }
    string PeripheralName { get; }
    string ID { get; set; }
    uint Reference { get; }
    Computer? Computer { get; }
    void Attach(Computer computer);
    void Detach(Computer computer);
    void Destroy();
}