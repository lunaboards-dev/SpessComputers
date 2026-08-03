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
        Console.WriteLine($"Create metatable {name}");
        if (Lua.luaL_newmetatable(L, name) == 0)
        {
            throw new Exception($"Metatable {name} already exists!");
        }
    }

    public override void FinishTable()
    {
        Lua.lua_pop(L, 1);
    }
}