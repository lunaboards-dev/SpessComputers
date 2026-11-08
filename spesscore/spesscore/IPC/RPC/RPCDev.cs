using System.Runtime.InteropServices;
using System.Text;
using spesscore.VM;
using static spesscore.VM.Lua;

namespace spesscore.IPC.RPC;

class RPCDev
{
    uint ObjRef;
    string Path;
    RPCMethod[] Methods;

    [StructLayout(LayoutKind.Sequential)]
    struct DevInfoHeader
    {
        public uint refid;
        public ushort mcount;
        public ushort pathlen;
    }

    static readonly unsafe int DI_Size = sizeof(DevInfoHeader);

    unsafe public RPCDev(byte * ptr, int size)
    {
        byte * max = ptr+size;
        if (size < DI_Size)
            throw new Exception("dev info smaller than devinfoheader");
        DevInfoHeader * dhdr = (DevInfoHeader *) ptr;
        if (DI_Size+dhdr->pathlen > size)
            throw new Exception("dev info can't contain path");
        Path = Encoding.UTF8.GetString(ptr+sizeof(DevInfoHeader), dhdr->pathlen);
        byte * head = ptr+sizeof(DevInfoHeader)+dhdr->pathlen;
        Methods = new RPCMethod[dhdr->mcount];
        ObjRef = dhdr->refid;
        Console.WriteLine($"device: {Path} ({dhdr->refid}): {dhdr->mcount} methods");
        for (int i = 0; i < dhdr->mcount; ++i)
        {
            Methods[i] = new RPCMethod(&head, max);
        }
    }

    public Dictionary<string, lua_CFunction> GetMethods(uint srcref)
    {
        Dictionary<string, lua_CFunction> funcs = new();
        foreach (var meth in Methods)
        {
            
        }
        return funcs;
    }
}