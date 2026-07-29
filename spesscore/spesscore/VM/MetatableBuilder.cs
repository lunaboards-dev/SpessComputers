namespace spesscore.VM;

class MetatableBuilder : TableBuilder
{
    string name;
    public MetatableBuilder(lua_State L, string name) : base(L)
    {
        this.name = name;
    }

    public override void CreateTable()
    {
        Lua.luaL_newmetatable(L, name);
    }

    public override void FinishTable()
    {
        Lua.lua_pop(L, 1);
    }
}