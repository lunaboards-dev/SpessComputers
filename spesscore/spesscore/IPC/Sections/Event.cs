
using System.Runtime.InteropServices;
using System.Text;
using spesscore.VM.Peripheral;

namespace spesscore.IPC.Sections;

class Event : IIPCSection
{
    public SectionType ID => SectionType.Event;
    public unsafe bool Read(byte* ptr, int Size, ref int Counter)
    {
        var str = new UnmanagedMemoryStream(ptr, Size);
        var br = new BinaryReader(str);
        byte[] _id = br.ReadBytes(36);
        string id = Encoding.ASCII.GetString(_id);
        string evname = br.ReadString();
        LuaValueList lvl = LuaValueList.Read(br);
        var p = SpessCore.Instance.GetPeripheral<IPeripheral>(id);
        p?.Computer?.events.Put(new VM.LuaSignal()
        {
            Name = evname,
            Sender = id,
            Values = lvl,
            Valid = true
        });
        return true;
    }

    public bool Write(MemoryStream stream, ref int Counter)
    {
        throw new NotImplementedException(); // valid crashout
    }
}