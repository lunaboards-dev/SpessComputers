using Payonel.LungoLua.Runtime;

namespace spesscore.NewVM.Lua;

class LuaCtx
{
    delegate int WrappedFunction(LuaCtx L);

    static int WrappedCall(lua_State L)
    {
        Boxed<WrappedFunction> func;
    }
}