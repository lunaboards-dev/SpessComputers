
using spesscore.VM.Peripheral;

namespace spesscore.IPC.Sections;

class CreateBIOS : IIPCSection
{
    public SectionType ID => SectionType.CreateBIOS;

    public unsafe bool Read(byte* ptr, int Size, ref int Counter)
    {
        uint * oref = (uint *)ptr;
        EEPROM rom;
        if (Size > 4)
        {
            string path = new((sbyte*)ptr, 4, Size-4);
            if (path != "" && path != "\0")
            {
                rom = new(*oref, path);
                goto eeprom_made;
            }
        }
        rom = new(*oref, []);
        eeprom_made:
        SpessCore.Instance.AddPeripheral(rom);
        return true;
    }

    public bool Write(MemoryStream stream, ref int Counter)
    {
        return false;
    }
}