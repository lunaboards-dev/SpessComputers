using System.Collections.ObjectModel;
using System.Text;
using System.Text.Unicode;

namespace spesscore.IPC.RPC;

struct RPCItem
{
    public static ReadOnlyDictionary<RPCArgType, string> TypeNames = new (new Dictionary<RPCArgType, string>()
    {
        {RPCArgType.Undefined, "UNDEFINED"},
        {RPCArgType.Int, "INTEGER"},
        {RPCArgType.Float, "FLOAT"},
        {RPCArgType.Bool, "BOOLEAN"},
        {RPCArgType.String, "STRING"},
        {RPCArgType.Blob, "BLOB"},
        {RPCArgType.List, "LIST"},
        {RPCArgType.Map, "MAP"},
        {RPCArgType.Null, "NULL"},
        {RPCArgType.Invalid, "INVALID"}
    });
    public RPCArgType Type;
    public object Data;

    public RPCItem(int i)
    {
        Data = i;
        Type = RPCArgType.Int;
    }

    public RPCItem(long l)
    {
        Data = l & 0xFFFF_FFFF;
        Type = RPCArgType.Int;
    }

    public RPCItem(float f)
    {
        Data = f;
        Type = RPCArgType.Float;
    }

    public RPCItem(double d)
    {
        Data = (float) d;
        Type = RPCArgType.Float;
    }

    public RPCItem(string s)
    {
        Data = Encoding.UTF8.GetBytes(s);
        Type = RPCArgType.String;
    }

    public RPCItem(byte[] b)
    {
        Data = b;
        Type = RPCArgType.String;
        foreach(byte v in b)
        {
            if (v < 32)
            {
                Type = RPCArgType.Blob;
                return;
            }
        }
        if (!Utf8.IsValid(b))
        {
            Type = RPCArgType.Blob;
        }
    }

    public RPCItem(bool b)
    {
        Data = b ? 1 : 0;
        Type = RPCArgType.Bool;
    }

    public static explicit operator bool(RPCItem i)
    {
        switch(i.Type)
        {
            case RPCArgType.Int:
            case RPCArgType.Bool:
            case RPCArgType.Float:
                return ((int) i.Data) != 0;
            default:
                throw new ArgumentException($"Cannot cast {TypeNames[i.Type]} to BOOLEAN");
        }
    }

    public static explicit operator int(RPCItem i)
    {
        switch(i.Type)
        {
            case RPCArgType.Int:
            case RPCArgType.Bool:
            case RPCArgType.Float:
                return (int) i.Data;
            default:
                throw new ArgumentException($"Cannot cast {TypeNames[i.Type]} to INTEGER");
        }
    }

    static Exception TypeError(RPCItem i, RPCArgType expected)
    {
        return new ArgumentException($"Cannot cast {TypeNames[i.Type]} to {TypeNames[expected]}");
    }

    public static explicit operator float(RPCItem i)
    {
        switch(i.Type)
        {
            case RPCArgType.Int:
            case RPCArgType.Bool:
            case RPCArgType.Float:
                return (float) i.Data;
            default:
                throw TypeError(i, RPCArgType.Float);
        }
    }

    public static explicit operator string(RPCItem i)
    {
        switch(i.Type)
        {
            case RPCArgType.String:
                return Encoding.UTF8.GetString((byte[]) i.Data);
            case RPCArgType.Blob:
                byte[] seq = (byte[]) i.Data;
                if (Utf8.IsValid(seq))
                {
                    goto case RPCArgType.String;
                }
                goto case RPCArgType.Invalid; // fucking C#
            case RPCArgType.Invalid:
            default:
                throw TypeError(i, RPCArgType.String);
        }
    }

    public static explicit operator byte[](RPCItem i)
    {
        switch(i.Type)
        {
            case RPCArgType.String:
            case RPCArgType.Blob:
                return (byte[])i.Data;
            default:
                throw TypeError(i, RPCArgType.Blob);
        }
    }

    public unsafe byte[] Serialize()
    {
        if (Type == RPCArgType.Int || Type == RPCArgType.Bool)
        {
            byte[] rtv = new byte[4];
            fixed(byte * ptr = rtv) ((int*) ptr)[0] = (int) Data;
            return rtv;
        } else if (Type == RPCArgType.Float)
        {
            byte[] rtv = new byte[4];
            fixed(byte * ptr = rtv) ((float*) ptr)[0] = (float) Data;
            return rtv;
        } else if (Type == RPCArgType.String)
        {
            byte[] v = (byte[])Data;
            byte[] rtv = new byte[v.Length+1];
            Array.Copy(v, rtv, v.Length);
            rtv[rtv.Length-1] = 0;
            return rtv;
        } else if (Type == RPCArgType.Blob)
        {
            byte[] v = (byte[])Data;
            byte[] rtv = new byte[v.Length+4];
            fixed(byte * ptr = rtv)
            {
                ((int*) ptr)[0] = v.Length;
            }
            Array.Copy(v, 0, rtv, 4, v.Length);
            return rtv;
        } else
        {
            return [];
        }
    }
}