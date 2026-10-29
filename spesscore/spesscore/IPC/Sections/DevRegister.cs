
namespace spesscore.IPC.Sections;

class DevRegister : IIPCSection
{
    public SectionType ID => SectionType.RegisterDev;

    public unsafe bool Read(byte* ptr, int Size, ref int Counter)
    {
        throw new NotImplementedException();
    }

    public bool Write(MemoryStream stream, ref int Counter)
    {
        throw new NotImplementedException();
    }
}