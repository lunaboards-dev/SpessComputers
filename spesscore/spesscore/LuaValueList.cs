namespace spesscore;

class LuaValueList
{
    enum ArgTypes
    {
        Null,
        Float,
        String,
        Array,
        Map
    }
    List<Object?> values = [];

    struct IVL
    {
        public List<Object> List = [];

        public IVL()
        {
        }

        public Dictionary<Object,Object> AsMap()
        {
            
        }
    }

    public static LuaValueList Read(BinaryReader br)
    {
        LuaValueList lvl = new();
        byte args = br.ReadByte();
        for (int i = 0; i < args; i+=4)
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
                        // read in array
                        break;
                    case ArgTypes.Map:
                        // read in map
                        break;
                    default:
                        // what
                        break;
                }
            }
        }
    }
}