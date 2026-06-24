using static spesscore.VM.Lua;
using static spesscore.VM.Helpers;

namespace spesscore.VM.Peripheral;

class EEPROM : AbstractPeripheral
{
    byte[] code;
    Dictionary<string,byte[]> config = [];
    int config_size = 0;
    Dictionary<string, IPeripheral.PeripheralCallback> callbacks;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private EEPROM() : base("eeprom")
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        callbacks = new()
        {
            {"code", Code},
            {"size", CodeSize},
            {"cfg_get", ConfigGet},
            {"cfg_set", ConfigSet},
            {"cfg_size", ConfigSize}
        };
    }

    public EEPROM(string path) : this()
    {
        var stream = File.OpenRead(path);
        code = new byte[stream.Length];
        stream.Read(code, 0, code.Length);
        stream.Close();
    }

    public EEPROM(byte[] bytes) : this()
    {
        code = bytes;
    }

    public override Dictionary<string, IPeripheral.PeripheralCallback> Callbacks => callbacks;

    public override void Destroy()
    {
        
    }

    int Code(lua_State L)
    {
        lua_pushbytebuffer(L, code);
        return 1;
    }

    int CodeSize(lua_State L)
    {
        lua_pushinteger(L, code.Length);
        return 1;
    }

    bool cfg_set(string key, byte[] val)
    {
        if (config.TryGetValue(key, out byte[]? oval))
        {
            int delta = val.Length - oval.Length;
            config_size += delta;
            config[key] = val;
        } else
        {
            config[key] = val;
            config_size += val.Length + key.Length;
        }
        return true; // this can't fail yet
    }

    int ConfigSet(lua_State L)
    {
        string key = luaL_checkstring(L, 2);
        byte[] value = luaL_checkbytebuffer(L, 3);
        lua_pushboolean(L, cfg_set(key, value) ? 1 : 0);
        return 1;
    }

    int ConfigGet(lua_State L)
    {
        string key = luaL_checkstring(L, 2);
        if (config.TryGetValue(key, out byte[]? val))
        {
            lua_pushbytebuffer(L, val);
            return 1;
        }
        return 0;
    }

    int ConfigSize(lua_State L)
    {
        lua_pushinteger(L, config_size);
        return 1;
    }
}