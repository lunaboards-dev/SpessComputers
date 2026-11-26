
using Payonel.LungoLua.Api;
using lua_State = Payonel.LungoLua.Runtime.lua_State;
using static Payonel.LungoLua.Api.LuaApi;
using static Payonel.LungoLua.Api.LuaAuxLib;
using static Payonel.LungoLua.Lib.Linit;
using static Payonel.LungoLua.Runtime.Ldo;
using static Payonel.LungoLua.Runtime.Lstate;
using static Payonel.LungoLua.Vm.Ldebug;
using spesscore.VM.Libraries;

namespace spesscore.NewVM;

class VM
{
    lua_State L;
    List<Library> Libs = [];

    public VM()
    {
    }

    void InitLuaState()
    {
        if (L != null)
        {
            lua_close(L);
        }
        L = luaL_newstate();
        luaL_openlibs(L);
    }
}