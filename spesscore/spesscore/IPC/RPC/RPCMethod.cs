using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using spesscore.VM;
using static spesscore.VM.Lua;

namespace spesscore.IPC.RPC;

public class RPCMethod
{
    internal RPCDev parent;
    internal uint refid;
    internal RPCArgType rtv;
    internal RPCArgType[] args;
    internal string path;
    internal string name;

    [StructLayout(LayoutKind.Sequential)]
    struct MethodInfoHeader
    {
        public uint refid;
        public RPCArgType rtv;
        public byte acount;
        public ushort pathlen;
    }

    

    static readonly unsafe int MI_Size = sizeof(MethodInfoHeader);
    unsafe internal RPCMethod(byte ** ptr, byte * max) {
        if (*ptr + MI_Size > max)
            throw new Exception("dev info can't fit method header");
        MethodInfoHeader * mhdr = (MethodInfoHeader *) *ptr;
        *ptr += MI_Size;
        refid = mhdr->refid;
        rtv = mhdr->rtv;
        args = new RPCArgType[mhdr->acount];
        if (*ptr + mhdr->acount + mhdr->pathlen > max)
            throw new Exception("dev info can't fit method header");
        var targs = new ReadOnlySpan<RPCArgType>(*ptr, mhdr->acount);
        args.CopyTo(args);
        *ptr += mhdr->acount;
        path = Encoding.UTF8.GetString(*ptr, mhdr->pathlen);
        *ptr += mhdr->pathlen;
        name = path[(path.LastIndexOf("/") + 1)..];
    }

    static readonly byte[] del_replacement = [0xe2, 0x90, 0xa1];

    // this also cleans the string for byond
    static byte[] BuildString(string str)
    {
        MemoryStream rtv = new();
        byte[] str_buffer = new byte[8];
        foreach (Rune rune in str.EnumerateRunes())
        {
            int rv = rune.Value;
            if (rv < 32 && rv != '\r' && rv != '\n' && rv != '\t')
            {
                var shift = new Rune(rv + 0x2400);
                int len = shift.EncodeToUtf8(str_buffer);
                rtv.Write(str_buffer, 0, len);
            } else if (rv == 0x7f) {
                rtv.Write(del_replacement);
            } else
            {
                int len = rune.EncodeToUtf8(str_buffer);
                rtv.Write(str_buffer, 0, len);
            }
        }
        rtv.WriteByte(0);
        return rtv.GetBuffer();
    }

    static RPCArgType RPCType(lua_State L, int idx, out int len)
    {
        len = -1;
        int lt = lua_type(L, idx);
        if (lt == LUA_TNIL)
        {
            return RPCArgType.Null;
        }
        if (lt == LUA_TNUMBER)
        {
            if (lua_isinteger(L, idx))
                return RPCArgType.Int;
            return RPCArgType.Float;
        } else if (lt == LUA_TSTRING)
        {
            byte[] tmp = lua_tobytebuffer(L, idx);
            for (int i=0; i < tmp.Length; ++i)
            {
                if (tmp[i] < 32) return RPCArgType.Blob;
            }
            if (!Utf8.IsValid(tmp)) return RPCArgType.Bool;
            return RPCArgType.String;
        } else if (lt == LUA_TBOOLEAN)
        {
            return RPCArgType.Bool;
        } else if (lt == LUA_TTABLE)
        {
            long tlen = luaL_len(L, idx);
            lua_pushnil(L);
            while (lua_next(L, idx))
            {
                if (lua_type(L, -2) != LUA_TNUMBER && !lua_isinteger(L, -2))
                {
                    lua_pop(L, 2);
                    return RPCArgType.Map;
                }
                long v = lua_tointeger(L, -2);
                if (v < 1 || v > tlen)
                {
                    lua_pop(L, 2);
                    return RPCArgType.Map;
                }
                lua_pop(L, 1);
            }
            lua_pop(L, 1);
            len = (int)tlen;
            return RPCArgType.List;
        }
        return RPCArgType.Invalid;
    }

    static byte[] ToBlob(byte[] bytes)
    {
        byte[] buffer = new byte[bytes.Length+4];
        BitConverter.TryWriteBytes(buffer, bytes.Length);
        Array.Copy(bytes, 0, buffer, 0, bytes.Length);
        return buffer;
    }

    static byte[] BasicSerializeAs(lua_State L, RPCArgType type, RPCArgType dtype, int idx)
    {
        switch (type)
        {
            case RPCArgType.Undefined:
                return BasicSerializeAs(L, dtype, dtype, idx);
            case RPCArgType.Int:
                return BitConverter.GetBytes((int)lua_tointeger(L, idx));
            case RPCArgType.Float:
                return BitConverter.GetBytes((float)lua_tonumber(L, idx));
            case RPCArgType.Bool:
                return BitConverter.GetBytes(lua_toboolean(L, idx));
            case RPCArgType.String:
                return BuildString(lua_tostring(L, idx));
            case RPCArgType.Blob:
                return ToBlob(lua_tobytebuffer(L, idx));
            case RPCArgType.Null:
                return [];
            default:
                luaL_error(L, $"internal serializer error: cannot serialize type {type}");
                return [];
        }
    }

    static int[] allowed_types = [LUA_TNUMBER, LUA_TSTRING, LUA_TBOOLEAN, LUA_TNIL];

    static byte[] SerializeList(lua_State L, int idx, int len)
    {
        MemoryStream rtv = new();
        byte[] types = new byte[len];
        byte[] _len = BitConverter.GetBytes(len);
        rtv.Write(_len);
        rtv.Write(types);
        for (int i = 0; i < len; i++)
        {
            lua_pushinteger(L, i+1);
            lua_gettable(L, idx);
            int type = lua_type(L, -1);
            if (!allowed_types.Contains(type))
            {
                types[i] = (byte)RPCArgType.Null;
            } else
            {
                RPCArgType t = RPCType(L, -1, out int vlen);
                types[i] = (byte)t;
                rtv.Write(BasicSerializeAs(L, t, t, -1));
            }
        }
        rtv.Seek(4, SeekOrigin.Begin);
        rtv.Write(types);
        return rtv.GetBuffer();
    }

    // this is a bit more scuffed and could probably be optimized some
    static byte[] SerializeMap(lua_State L, int idx)
    {
        List<byte> types = [];
        List<byte[]> vals = [];
        int needed_size = 0;
        lua_pushnil(L);
        while (lua_next(L, idx))
        {
            int ktype = lua_type(L, -2);
            int vtype = lua_type(L, -1);
            if (!allowed_types.Contains(ktype))
            {
                types.Add((byte) RPCArgType.Null);
                vals.Add([]);
            } else
            {
                RPCArgType t = RPCType(L, -2, out int vlen);
                types.Add((byte)t);
                byte[] val = BasicSerializeAs(L, t, t, -2);
                vals.Add(val);
                needed_size += val.Length;
            }

            if (!allowed_types.Contains(vtype))
            {
                types.Add((byte) RPCArgType.Null);
                vals.Add([]);
            } else
            {
                RPCArgType t = RPCType(L, -1, out int vlen);
                types.Add((byte)t);
                byte[] val = BasicSerializeAs(L, t, t, -1);
                vals.Add(val);
                needed_size += val.Length;
            }
        }
        int len = types.Count;
        byte[] buffer = new byte[4+len+needed_size];
        BitConverter.TryWriteBytes(buffer, len);
        int offset = len+4;
        for (int i = 0; i < len; ++i)
        {
            buffer[i+4] = types[i];
            int vlen = vals[i].Length;
            Array.Copy(vals[i], 0, buffer, offset, vlen);
            offset += vlen;
        }
        return buffer;
    }

    readonly static ReadOnlyDictionary<RPCArgType,RPCArgType[]> Compatibility = new(new Dictionary<RPCArgType,RPCArgType[]>()
    {
        {RPCArgType.Undefined, [RPCArgType.Undefined, RPCArgType.Int, RPCArgType.Float, RPCArgType.Bool, RPCArgType.String, RPCArgType.Blob, RPCArgType.List, RPCArgType.Map, RPCArgType.Null]},
        {RPCArgType.Int, [RPCArgType.Int]},
        {RPCArgType.Float, [RPCArgType.Int, RPCArgType.Float]},
        {RPCArgType.Bool, [RPCArgType.Bool]},
        {RPCArgType.String, [RPCArgType.Int, RPCArgType.Float, RPCArgType.String]},
        {RPCArgType.Blob, [RPCArgType.String, RPCArgType.Blob]},
        {RPCArgType.List, [RPCArgType.String]},
        {RPCArgType.Map, [RPCArgType.List, RPCArgType.Map]},
        {RPCArgType.Null, [RPCArgType.Null]}
    });

    internal lua_CFunction GetFunc(int sref)
    {
        return (L) =>
        {
            int largs = lua_gettop(L);
            for (int i=0; i < args.Length; i++)
            {
                var rpc_t = RPCType(L, i+1, out int len);
                var argt = args[i];
                if (!Compatibility.TryGetValue(argt & RPCArgType.Type, out RPCArgType[] compat) || !compat.Contains(rpc_t))
                {
                    luaL_typeerror(L, i+1, $"{argt & RPCArgType.Type}-like");
                }
            }
            return lua_yield(L, 0);
        };
    }
}