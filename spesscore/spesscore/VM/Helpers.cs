using System.Runtime.InteropServices;
using static spesscore.VM.Lua;
using static spesscore.VM.Helpers;

namespace spesscore.VM;

static class Helpers
{
    unsafe public static void lua_PushObjectManaged<T>(lua_State L, T obj)
    {
        if (obj == null)
        {
            lua_pushnil(L);
            return;
        }
        var hand = GCHandle.Alloc(obj);
        var box = lua_newuserdata(L, (ulong)sizeof(nuint));
        var ptr = (nint*)box;
        *ptr = GCHandle.ToIntPtr(hand);
        //lua_pushlightuserdata(L, (nuint)GCHandle.ToIntPtr(hand));
    }

    unsafe public static void lua_PushTemporaryObject<T>(lua_State L, T obj)
    {
        lua_PushObjectManaged(L, obj);
        lua_newtable(L);
        lua_pushstring(L, "__gc");
        lua_pushcfunction(L, ReleaseObjectDelegate);
        lua_settable(L, -3);
        lua_setmetatable(L, -2);
    }

    public static lua_CFunction ReleaseObjectDelegate = lua_ReleaseObject;
    // gc hook
    unsafe public static int lua_ReleaseObject(lua_State L)
    {
        if (lua_type(L, 1) == LUA_TUSERDATA)
        {
            var box = lua_touserdata(L, 1);
            var ptr = (nint*)box;
            var hand = *ptr;
            GCHandle.FromIntPtr(hand).Free();
        }
        return 0;
    }

    unsafe public static T? lua_ToObject<T>(lua_State L, int idx)
    {
        if (!lua_isuserdata(L, idx)) return default;
        var box = lua_touserdata(L, idx);
        if (box == nuint.Zero) return default;
        var ptr = (nint*)box;
        var hand = *ptr;
        if (hand == nint.Zero) return default;
        var gch = GCHandle.FromIntPtr(hand);
        if (!gch.IsAllocated) return default;
        return (T)gch.Target;
    }

    // byte array methods because SOMEONE didn't think of it
    
}