
using System.Text.Unicode;
using spesscore.IPC.RPC;
using static spesscore.VM.Lua;

namespace spesscore.VM.Peripheral;

class DMPeripheral : IPeripheral
{
    public Dictionary<string, lua_CFunction> Callbacks => throw new NotImplementedException();

    public string PeripheralName => throw new NotImplementedException();

    public string ID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public Computer? Computer => throw new NotImplementedException();

    public uint Reference => throw new NotImplementedException();

    static Dictionary<RPCArgType, int> TypeMappingFrom = new()
    {
        {RPCArgType.Undefined, -1},
        {RPCArgType.Int, LUA_TNUMBER},
        {RPCArgType.Float, LUA_TNUMBER},
        {RPCArgType.Bool, LUA_TBOOLEAN},
        {RPCArgType.String, LUA_TSTRING},
        {RPCArgType.Blob, LUA_TSTRING},
        {RPCArgType.List, LUA_TTABLE},
        {RPCArgType.Map, LUA_TTABLE}
    };

    static RPCArgType RPCType(lua_State L, int idx)
    {
        int lt = lua_type(L, idx);
        if (L == LUA_TNUMBER)
        {
            if (lua_isinteger(L, idx))
                return RPCArgType.Int;
            return RPCArgType.Float;
        } else if (L == LUA_TSTRING)
        {
            byte[] tmp = lua_tobytebuffer(L, idx);
            for (int i=0; i < tmp.Length; ++i)
            {
                if (tmp[i] < 32) return RPCArgType.Blob;
            }
            if (!Utf8.IsValid(tmp)) return RPCArgType.Bool;
            return RPCArgType.String;
        } else if (L == LUA_TBOOLEAN)
        {
            return RPCArgType.Bool;
        } else if (L == LUA_TTABLE)
        {
            // FUCK
        }
        return RPCArgType.Invalid;
    }

    lua_CFunction AddMethod(string path, RPCArgType[] args)
    {
        return (L) =>
        {
            int top = lua_gettop(L);
            if (top != args.Length)
            {
                luaL_typeerror(L, top+1, lua_typename(L, TypeMappingFrom[args[top+1]]) ?? "unknown");
            } else
            {
                
            }
            return 1;
        };
    }

    void StartRPCCall(string func, object[] args)
    {
        
    }

    public void Attach(Computer computer)
    {
        throw new NotImplementedException();
    }

    public void Destroy()
    {
        // this is never called, as it would handled DM-side.
    }

    public void Detach(Computer computer)
    {
        throw new NotImplementedException();
    }
}