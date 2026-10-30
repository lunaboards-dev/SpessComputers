
using System.Runtime.InteropServices;
using System.Text;
using spesscore.IPC.RPC;

namespace spesscore.IPC.Sections;

class DevRegister : IIPCSection
{
    public SectionType ID => SectionType.RegisterDev;

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

    struct DevInfo
    {
        public uint refid;
        public string path;
        public MethodInfo[] methods;
    }

    struct MethodInfo
    {
        public uint refid;
        public RPCArgType rtv;
        public RPCArgType[] args;
        public string path;
    }

    public unsafe bool Read(byte* ptr, int Size, ref int Counter)
    {
        DevInfoHeader * dhdr = (DevInfoHeader *) ptr;
        DevInfo dev = new();
        dev.path = Encoding.UTF8.GetString(ptr+sizeof(DevInfoHeader), dhdr->pathlen);
        byte * head = ptr+sizeof(DevInfoHeader)+dhdr->pathlen;
        dev.methods = new MethodInfo[dhdr->mcount];
        Console.WriteLine($"device: {dev.path} ({dhdr->refid}): {dhdr->mcount} methods");
        for (int i = 0; i < dhdr->mcount; ++i)
        {
            MethodInfoHeader * mhdr = (MethodInfoHeader *) head;
            head += sizeof(MethodInfoHeader);
            dev.methods[i].refid = mhdr->refid;
            dev.methods[i].rtv = mhdr->rtv;
            dev.methods[i].args = new RPCArgType[mhdr->acount];
            var args = new ReadOnlySpan<RPCArgType>(head, mhdr->acount);
            args.CopyTo(dev.methods[i].args);
            head += mhdr->acount;
            dev.methods[i].path = Encoding.UTF8.GetString(head, mhdr->pathlen);
            head += mhdr->pathlen;
            Console.WriteLine($"method: {dev.methods[i].path} ({dev.methods[i].refid}): {mhdr->acount} args");
        }
        return true;
    }

    public bool Write(MemoryStream stream, ref int Counter)
    {
        throw new NotImplementedException();
    }
}