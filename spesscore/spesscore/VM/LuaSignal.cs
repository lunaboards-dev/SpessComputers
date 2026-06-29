using System.Runtime.InteropServices;
using static spesscore.VM.Lua;
using static spesscore.VM.Helpers;

namespace spesscore.VM;

struct LuaSignal
{
    public bool Valid;
    public string Name;
    public string Sender;
    public LuaValueList Values;

    public int Push(lua_State L)
    {
        lua_pushstring(L, Name);
        lua_pushstring(L, Sender);
        Values.PushArgs(L);
        return Values.Count+2;
    }
}