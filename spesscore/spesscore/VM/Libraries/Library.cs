namespace spesscore.VM.Libraries;

using static spesscore.VM.Lua;
using static spesscore.VM.Helpers;

abstract class Library(string name)
{
    public string Name => name;
    abstract public Dictionary<string, lua_CFunction> Functions { get; }

    public void Push(lua_State L)
    {
        lua_newtable(L);
        foreach (var pair in Functions)
        {
            lua_pushstring(L, pair.Key);
            lua_pushcfunction(L, pair.Value);
            lua_settable(L, -3);
        }
        lua_setglobal(L, name);
    }
}