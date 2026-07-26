using spesscore.VM;
using static spesscore.VM.Lua;
using static spesscore.VM.Helpers;
using spesscore.VM.Peripheral;

class NetworkCard : AbstractPeripheral
{
    int network;
    public NetworkCard(bool tcomms) : base("net")
    {
        
    }

    public override Dictionary<string, IPeripheral.PeripheralCallback> Callbacks => throw new NotImplementedException();

    public override void Destroy()
    {
        throw new NotImplementedException();
    }

    public 

    int Send(lua_State L)
    {

        return 0;
    }

    int Broadcast(lua_State L)
    {
        return 0;
    }

    int Listen(lua_State L)
    {
        return 0;
    }

    int Ignore(lua_State L)
    {
        return 0;
    }

    int GetChannel(lua_State L)
    {
        return 0;
    }

    int SetChannel(lua_State L)
    {
        return 0;
    }

    int EnableSnooping(lua_State L)
    {
        return 0;
    }

    void Send(string src, ushort port, byte[] data)
    {
        
    }

    void SnooperSend(string src, ushort port, byte[] data, string dst)
    {
        
    }
}