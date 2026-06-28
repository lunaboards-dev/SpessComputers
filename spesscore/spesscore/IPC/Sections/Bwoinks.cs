namespace spesscore.IPC.Sections;

class Bwoinks : IIPCSection
{
    public SectionType ID => SectionType.Bwoink;

    public unsafe bool Read(byte* ptr, int Size, ref int Counter)
    {
        // we shouldn't fuckin have this sent to us
        return false;
    }

    public bool Write(MemoryStream stream, ref int Counter)
    {
        if (Counter > 0) return false;
        var br = new BinaryWriter(stream);
        var bwoinks = SpessCore.Instance?.Bwoinks;
        if (bwoinks == null) return false;
        br.Write(bwoinks.Count);
        foreach (var bwoink in bwoinks)
        {
            br.Write(bwoink);
        }
        Counter++;
        return true;
    }
}