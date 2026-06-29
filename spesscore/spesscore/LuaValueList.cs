using spesscore.VM;
using static spesscore.VM.Lua;

namespace spesscore;

class LuaValueList
{
    enum ArgTypes
    {
        Null,
        Float,
        String,
        Array,
        Map,
        True,
        False
    }
    List<object?> values = [];
    public int Count => values.Count;

    struct IVL
    {
        public List<object?> List = [];

        public IVL()
        {
        }

        public Dictionary<object, object?> AsMap()
        {
            Dictionary<object,object?> rtv = [];
            for (int i = 0; i < rtv.Count; i++)
            {
                var l = List[i]; // realistically this shouldn't ever be a problem
                if (l == null) continue;
                var r = List[i+1];
                rtv[l] = r;
            }
            return rtv;
        }

        public IVL Read(BinaryReader br)
        {
            uint count = br.ReadUInt32();
            int types = 0;
            for (int i = 0; i < count; i+=6) {
                for (int j = 0; j < 3; j++)
                    types = (types << 8) | br.ReadByte();
                for (int j = 0; (j < 4) && (i+j < count); j++)
                {
                    ArgTypes type = (ArgTypes)(types & 0x1f);
                    types >>= 3;
                    switch (type)
                    {
                        case ArgTypes.Null:
                            List.Add(null);
                            break;
                        case ArgTypes.Float:
                            List.Add(br.ReadSingle()); // why the FUCK is it not readfloat
                            break;
                        case ArgTypes.String:
                            List.Add(br.ReadString());
                            break;
                        case ArgTypes.Array:
                            List.Add(new IVL().Read(br).List);
                            break;
                        case ArgTypes.Map:
                            List.Add(new IVL().Read(br).AsMap());
                            break;
                        case ArgTypes.True:
                            List.Add(true);
                            break;
                        case ArgTypes.False:
                            List.Add(false);
                            break;
                        default:
                            // what
                            break;
                    }
                }
            }
            return this;
        }
    }

    public static LuaValueList Read(BinaryReader br)
    {
        LuaValueList lvl = new();
        byte args = br.ReadByte();
        for (int i = 0; i < args; i+=6)
        {
            int types = 0;
            for (int j = 0; j < 3; j++)
                types = (types << 8) | br.ReadByte();
            for (int j = 0; (j < 4) && (i+j < args); j++)
            {
                ArgTypes type = (ArgTypes)(types & 0x1f);
                types >>= 3;
                switch (type)
                {
                    case ArgTypes.Null:
                        lvl.values.Add(null);
                        break;
                    case ArgTypes.Float:
                        lvl.values.Add(br.ReadSingle()); // why the FUCK is it not readfloat
                        break;
                    case ArgTypes.String:
                        lvl.values.Add(br.ReadString());
                        break;
                    case ArgTypes.Array:
                        lvl.values.Add(new IVL().Read(br).List);
                        break;
                    case ArgTypes.Map:
                        lvl.values.Add(new IVL().Read(br).AsMap());
                        break;
                    case ArgTypes.True:
                        lvl.values.Add(true);
                        break;
                    case ArgTypes.False:
                        lvl.values.Add(false);
                        break;
                    default:
                        // what
                        break;
                }
            }
        }
        return lvl;
    }

    static void PushObj(object o, lua_State L)
    {
        if (o is float f)
            lua_pushnumber(L, f);
        else if (o is bool b)
            lua_pushboolean(L, b ? 1 : 0);
        else if (o is string s)
            lua_pushstring(L, s);
        else if (o is List<object?> l)
            PushList(l, L);
        else if (o is Dictionary<object,object?> m)
            PushMap(m, L);
    }

    static void PushList(List<object?> list, lua_State L)
    {
        lua_newtable(L);
        int pos = 0;
        foreach (var o in list)
        {
            pos++;
            if (o == null) continue;
            else PushObj(o, L);
            lua_rawseti(L, -2, pos);
        }
    }

    static void PushMap(Dictionary<object,object?> map, lua_State L)
    {
        lua_newtable(L);
        foreach (var pair in map)
        {
            if (pair.Value == null) continue;
            PushObj(pair.Key, L);
            PushObj(pair.Value, L);
            lua_rawset(L, -3);
        }
    }

    public void PushArgs(lua_State L)
    {
        foreach (var obj in values)
        {
            if (obj == null) lua_pushnil(L);
            else PushObj(obj, L);
        }
    }

    public void PushList(lua_State L)
    {
        PushList(values, L);
    }
}