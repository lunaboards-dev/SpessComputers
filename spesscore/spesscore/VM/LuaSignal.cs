using System.Runtime.InteropServices;
using static spesscore.VM.Lua;
using static spesscore.VM.Helpers;

namespace spesscore.VM;

struct LuaSignal
{
    public string Name;
    public string Sender;
    public object[] args;

    public int Push(lua_State L)
    {
        lua_pushstring(L, Name);
        lua_pushstring(L, Sender);
        foreach (object o in args)
        {
            if (o is string str)
            {
                lua_pushstring(L, str);
            } else if (o is long ival)
            {
                lua_pushinteger(L, ival);
            } else if (o is double dval)
            {
                lua_pushnumber(L, dval);
            } else if (o is bool bval)
            {
                lua_pushboolean(L, bval ? 1 : 0);
            } else
            {
                lua_pushnil(L);
            }
        }
        return args.Length+2;
    }
}