
using spesscore.VM.Peripheral;

namespace spesscore.IPC.Sections;

class CreateTTY : IIPCSection
{
    public SectionType ID => SectionType.CreateTTY;

    public unsafe bool Read(byte* ptr, int Size, ref int Counter)
    {
        if (Size < sizeof(uint)) return false;
        uint * oref = (uint *)ptr;
        TTY tty = new(*oref);
        SpessCore.Instance.AddPeripheral(tty);
        return true;
    }

    public bool Write(MemoryStream stream, ref int Counter)
    {
        return false;
    }
}