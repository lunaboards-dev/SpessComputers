namespace spesscore.IPC.Sections;
interface IIPCSection
{
    SectionType ID { get; }
    unsafe bool Read(byte* ptr, int Size, ref int Counter);
    bool Write(MemoryStream stream, ref int Counter);
}