namespace spesscore.VM.Libraries;

using static spesscore.VM.Lua;
using static spesscore.VM.Helpers;
using spesscore.VM.Peripheral;
using System.Collections.Generic;

class PeripheralAPI : Library
{
    Computer Computer;
    Dictionary<string, lua_CFunction> funcs;
    public PeripheralAPI(Computer comp) : base("peripheral")
    {
        Computer = comp;
        funcs = new()
        {
            {"call", Call},
            {"methods", Methods},
            {"type", Type},
            {"list", List}
        };
    }

    public override Dictionary<string, lua_CFunction> Functions => funcs;

    // low memory overhead call
    int Call(lua_State L)
    {
        string id = luaL_checkstring(L, 1);
        string func_name = luaL_checkstring(L, 2);
        if (id == null || func_name == null) return luaL_error(L, "internal error (null check failed)");
        var perf = Computer.GetPeripheral(id);
        if (perf == null) return luaL_error(L, "peripheral not found");
        if (perf.Callbacks.TryGetValue(func_name, out lua_CFunction func))
        {
            lua_remove(L, 1); // remove bottom of stack. we only need to do this once.
            return func(L);
        }
        return luaL_error(L, "invalid method");
    }

    int Methods(lua_State L)
    {
        string id = luaL_checkstring(L, 1);
        if (id == null) return luaL_error(L, "internal error (null check failed");
        var perf = Computer.GetPeripheral(id);
        if (perf == null) return luaL_error(L, "peripheral not found");
        var rtv = new TableBuilder(L);
        foreach (var pair in perf.Callbacks)
        {
            rtv.AddString(pair.Key);
        }
        rtv.Close();
        return 1;
    }

    int Type(lua_State L)
    {
        string id = luaL_checkstring(L, 1);
        if (id == null) return luaL_error(L, "internal error (null check failed");
        var perf = Computer.GetPeripheral(id);
        if (perf == null) return luaL_error(L, "peripheral not found");
        lua_pushstring(L, perf.PeripheralName);
        return 1;
    }

    lua_CFunction ListCallDel = ListCallback;
    static int ListCallback(lua_State L)
    {
        List<IPeripheral>? p = lua_ToObject<List<IPeripheral>>(L, lua_upvalueindex(1));
        long index = lua_tointeger(L, lua_upvalueindex(2));
        if (p == null) return luaL_error(L, "internal error (ref is null)");
        if (index >= p.Count) return 0;
        var perf = p[(int)index++];

        lua_pushinteger(L, index);
        lua_replace(L, lua_upvalueindex(2));

        lua_pushstring(L, perf.ID);
        lua_pushstring(L, perf.PeripheralName);
        return 2;
    }

    int List(lua_State L)
    {
        List<IPeripheral> perfs = Computer.Peripherals;
        if (lua_type(L, 1) == LUA_TSTRING)
        {
            string filt = lua_tostring(L, 1);
            perfs = Computer.Peripherals.Where((c) => c.PeripheralName.StartsWith(filt))
                    .ToList();
        }
        lua_PushTemporaryObject(L, perfs);
        lua_pushinteger(L, 0);
        lua_pushcclosure(L, ListCallDel, 2);
        return 1;
    }
}