namespace spesscore.VM;

using static spesscore.VM.Lua;
using static spesscore.VM.Helpers;

class TableBuilder
{
    public class SubTableBuilder : TableBuilder
    {
        TableBuilder parent;
        internal SubTableBuilder(lua_State L, TableBuilder parent) : base(L)
        {
            this.parent = parent;
        }

        public override void FinishTable()
        {
            lua_settable(L, -3);
            parent.waiting_table = false;
        }
    }
    internal lua_State L;
    int length = 0;
    bool waiting_table;
    bool complete;
    
    bool InsertValid => !(waiting_table || complete);
    public TableBuilder(lua_State L)
    {
        this.L = L;
        CreateTable();
    }
    public virtual void CreateTable()
    {
        lua_newtable(L);
    }

    public virtual void FinishTable()
    {
        // nothing
    }

    public void SetString(string key, string value)
    {
        if (!InsertValid) throw new Exception("can't write to this table right now");
        lua_pushstring(L, key);
        lua_pushstring(L, value);
        lua_settable(L, -3);
    }

    public void SetInt(string key, int value)
    {
        if (!InsertValid) throw new Exception("can't write to this table right now");
        lua_pushstring(L, key);
        lua_pushinteger(L, value);
        lua_settable(L, -3);
    }

    public void SetFloat(string key, double value)
    {
        if (!InsertValid) throw new Exception("can't write to this table right now");
        lua_pushstring(L, key);
        lua_pushnumber(L, value);
        lua_settable(L, -3);
    }

    public void SetBoolean(string key, bool value)
    {
        if (!InsertValid) throw new Exception("can't write to this table right now");
        lua_pushstring(L, key);
        lua_pushboolean(L, value ? 1 : 0);
        lua_settable(L, -3);
    }

    public void SetFunction(string key, lua_CFunction func)
    {
        if (!InsertValid) throw new Exception("can't write to this table right now");
        lua_pushstring(L, key);
        lua_pushcfunction(L, func);
        lua_settable(L, -3);
    }

    public TableBuilder SetTable(string key)
    {
        if (!InsertValid) throw new Exception("can't write to this table right now");
        lua_pushstring(L, key);
        waiting_table = true;
        return new SubTableBuilder(L, this);
    }

    public void AddString(string value)
    {
        if (!InsertValid) throw new Exception("can't write to this table right now");
        lua_pushinteger(L, ++length);
        lua_pushstring(L, value);
        lua_settable(L, -3);
    }

    public void AddInt(int value)
    {
        if (!InsertValid) throw new Exception("can't write to this table right now");
        lua_pushinteger(L, ++length);
        lua_pushinteger(L, value);
        lua_settable(L, -3);
    }

    public void AddFloat(double value)
    {
        if (!InsertValid) throw new Exception("can't write to this table right now");
        lua_pushinteger(L, ++length);
        lua_pushnumber(L, value);
        lua_settable(L, -3);
    }

    public void AddBoolean(string key, bool value)
    {
        if (!InsertValid) throw new Exception("can't write to this table right now");
        lua_pushinteger(L, ++length);
        lua_pushboolean(L, value ? 1 : 0);
        lua_settable(L, -3);
    }

    public void AddFunction(lua_CFunction func)
    {
        if (!InsertValid) throw new Exception("can't write to this table right now");
        lua_pushinteger(L, ++length);
        lua_pushcfunction(L, func);
        lua_settable(L, -3);
    }

    public TableBuilder AddTable(string key)
    {
        if (!InsertValid) throw new Exception("can't write to this table right now");
        lua_pushinteger(L, ++length);
        waiting_table = true;
        return new SubTableBuilder(L, this);
    }

    public void Close()
    {
        FinishTable();
    }
}