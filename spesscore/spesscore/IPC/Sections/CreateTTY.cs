
using spesscore.VM.Peripheral;

namespace spesscore.IPC.Sections;

class CreateTTY : IIPCSection
{
    public SectionType ID => SectionType.CreateTTY;

    public unsafe bool Read(byte* ptr, int Size, ref int Counter)
    {
        if (Size < sizeof(uint)) {
            SpessCore.Instance.Bwoink("Bad CreateTTY packet: size is less than sizeof(uint)");
            return false;
        }
        uint * oref = (uint *)ptr;
        TTY tty = new(*oref);
        SpessCore.Instance.AddPeripheral(tty);
        Console.WriteLine($"Created TTY for ref {*oref}");
        return true;
    }

    public bool Write(MemoryStream stream, ref int Counter)
    {
        return false;
    }
}