using System.Data.SQLite;
using static spesscore.VM.Lua;
using static spesscore.VM.Helpers;
using spesscore.VM;
using System.Data.Common;

static class QueryReader
{
    public static int Push(lua_State L, SQLiteDataReader reader)
    {
        lua_PushObjectManaged(L, reader);
        luaL_setmetatable(L, "SqlQueryReader");
        return 1;
    }

    public static void InitLib(lua_State L)
    {
        var mt = new TableBuilder(L, "SqlQueryReader");
        var it = mt.SetTable("__index");
        it.SetFunction("empty", EmptyDel);
        it.SetFunction("read", ReadDel);
        it.SetFunction("rows", RowsDel);
        it.SetFunction("next", NextDel);
        it.SetFunction("values", ValuesDel);
        it.Close();
        mt.SetFunction("__gc", ReleaseObjectDelegate);
        mt.Close();
    }

    static lua_CFunction EmptyDel = Empty;
    static int Empty(lua_State L)
    {
        var reader = lua_ToObject<SQLiteDataReader>(L, 1);
        if (reader == null || !reader.HasRows)
        {
            lua_pushboolean(L, 1);
        } else
        {
            lua_pushboolean(L, 0);
        }
        return 1;
    }

    static void AutomagicSQLiteToLua(lua_State L, SQLiteDataReader reader, int i)
    {
        var rdt = reader.GetFieldAffinity(i);
        switch (rdt)
        {
            case TypeAffinity.Uninitialized:
            case TypeAffinity.Null:
                lua_pushnil(L);
                break;
            case TypeAffinity.Int64:
                lua_pushinteger(L, reader.GetInt64(i));
                break;
            case TypeAffinity.Double:
                lua_pushnumber(L, reader.GetDouble(i));
                break;
            case TypeAffinity.Text:
            case TypeAffinity.DateTime:
                lua_pushstring(L, reader.GetString(i));
                break;
            case TypeAffinity.Blob:
            case TypeAffinity.None:
                byte[] buf = (byte[])reader[i];
                lua_pushbytebuffer(L, buf);
                break;
            default:
                lua_pushnil(L);
                break;
        }
    }

    static int InternalRead(lua_State L, int d_idx, int t_idx)
    {
        var reader = lua_ToObject<SQLiteDataReader>(L, d_idx);
        if (reader == null) return luaL_error(L, "internal error: SQLiteDataReader is null");
        if (!reader.Read()) return 0;
        /* if (lua_type(L, t_idx) == LUA_TTABLE) lua_pushvalue(L, 2);
        else lua_newtable(L); */
        lua_newtable(L);
        for (int i=0; i<reader.FieldCount; ++i)
        {
            lua_pushstring(L, reader.GetName(i));
            AutomagicSQLiteToLua(L, reader, i);
            lua_settable(L, -3);
        }
        return 1;
    }

    static lua_CFunction ReadDel = Read;
    static int Read(lua_State L)
    {
        return InternalRead(L, 1, 2);
    }

    static lua_CFunction ReadIterDel = ReadIter;
    static int ReadIter(lua_State L)
    {
        return InternalRead(L, lua_upvalueindex(1), 1);
    }

    static lua_CFunction RowsDel = Rows;
    static int Rows(lua_State L)
    {
        lua_pushvalue(L, 1);
        lua_pushcclosure(L, ReadIterDel, 1);
        lua_newtable(L);
        return 2;
    }

    static int InternalNext(lua_State L, int idx)
    {
        var reader = lua_ToObject<SQLiteDataReader>(L, idx);
        if (reader == null) return luaL_error(L, "internal error: SQLiteDataReader is null");
        if (!reader.Read()) return 0;
        for (int i=0; i<reader.FieldCount; ++i)
        {
            AutomagicSQLiteToLua(L, reader, i);
        }
        return reader.FieldCount;
    }

    static lua_CFunction NextDel = Next;
    static int Next(lua_State L)
    {
        return InternalNext(L, 1);
    }

    static lua_CFunction NextIterDel = NextIter;
    static int NextIter(lua_State L)
    {
        return InternalNext(L, lua_upvalueindex(1));
    }

    static lua_CFunction ValuesDel = Values;
    static int Values(lua_State L)
    {
        lua_pushvalue(L, 1);
        lua_pushcclosure(L, NextIterDel, 1);
        return 1;
    }
}