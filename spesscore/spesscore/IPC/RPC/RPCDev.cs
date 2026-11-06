using System.Runtime.InteropServices;
using System.Text;

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

    [StructLayout(LayoutKind.Sequential)]
    struct MethodInfoHeader
    {
        public uint refid;
        public RPCArgType rtv;
        public byte acount;
        public ushort pathlen;
    }

    unsafe public RPCDev(byte * ptr, int size)
    {
        DevInfoHeader * dhdr = (DevInfoHeader *) ptr;
        Path = Encoding.UTF8.GetString(ptr+sizeof(DevInfoHeader), dhdr->pathlen);
        byte * head = ptr+sizeof(DevInfoHeader)+dhdr->pathlen;
        Methods = new RPCMethod[dhdr->mcount];
        ObjRef = dhdr->refid;
        Console.WriteLine($"device: {Path} ({dhdr->refid}): {dhdr->mcount} methods");
        for (int i = 0; i < dhdr->mcount; ++i)
        {
            /* MethodInfoHeader * mhdr = (MethodInfoHeader *) head;
            head += sizeof(MethodInfoHeader);
            dev.methods[i].refid = mhdr->refid;
            dev.methods[i].rtv = mhdr->rtv;
            dev.methods[i].args = new RPCArgType[mhdr->acount];
            var args = new ReadOnlySpan<RPCArgType>(head, mhdr->acount);
            args.CopyTo(dev.methods[i].args);
            head += mhdr->acount;
            dev.methods[i].path = Encoding.UTF8.GetString(head, mhdr->pathlen);
            head += mhdr->pathlen;
            Console.WriteLine($"method: {dev.methods[i].path} ({dev.methods[i].refid}): {mhdr->acount} args"); */
        }
    }
}