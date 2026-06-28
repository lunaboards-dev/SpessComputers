
using System.Runtime.InteropServices;
using System.Text;

namespace spesscore.IPC.Sections;

class Event : IIPCSection
{
    public SectionType ID => SectionType.Event;
    enum ArgTypes
    {
        Float,
        String,
        Array,
        Map
    }
    public unsafe bool Read(byte* ptr, int Size, ref int Counter)
    {
        var str = new UnmanagedMemoryStream(ptr, Size);
        var br = new BinaryReader(str);
        byte[] _id = br.ReadBytes(36);
        string id = Encoding.ASCII.GetString(_id);
        string evname = br.ReadString();
        byte args = br.ReadByte();
        for (int i = 0; i < args; i+=4)
        {
            
        }
    }

    public bool Write(MemoryStream stream, ref int Counter)
    {
        throw new NotImplementedException();
    }
}