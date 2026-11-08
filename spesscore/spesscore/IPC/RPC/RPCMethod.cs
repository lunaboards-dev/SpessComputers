using System.Runtime.InteropServices;
using System.Text;
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

    internal lua_CFunction GetFunc(int sref)
    {
        
    }
}