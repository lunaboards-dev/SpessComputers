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

    static RPCArgType RPCType(lua_State L, int idx, out int len)
    {
        len = -1;
        int lt = lua_type(L, idx);
        if (L == LUA_TNUMBER)
        {
            if (lua_isinteger(L, idx))
                return RPCArgType.Int;
            return RPCArgType.Float;
        } else if (L == LUA_TSTRING)
        {
            byte[] tmp = lua_tobytebuffer(L, idx);
            for (int i=0; i < tmp.Length; ++i)
            {
                if (tmp[i] < 32) return RPCArgType.Blob;
            }
            if (!Utf8.IsValid(tmp)) return RPCArgType.Bool;
            return RPCArgType.String;
        } else if (L == LUA_TBOOLEAN)
        {
            return RPCArgType.Bool;
        } else if (L == LUA_TTABLE)
        {
            // FUCK
            long tlen = luaL_len(L, idx);
        }
        return RPCArgType.Invalid;
    }

    internal lua_CFunction GetFunc(int sref)
    {
        return (L) =>
        {
            int args = lua_gettop(L);
            return lua_yield(L, 0);
        };
    }
}