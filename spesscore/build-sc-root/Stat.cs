struct SCStat
{
    public static byte Dir = 0x1;
    public static byte File = 0x2;
    public static byte Link = 0x3;
    public static byte Unknown = 0xF;
    public string RealPath;
    public string Path;
    public short Perms;
    public short Owner;
    public short Group;
    public byte Type;
    public string Target;
    public bool Virtual;
}