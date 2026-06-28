
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace spesscore.IPC.Sections;

class GameState : IIPCSection
{
    public SectionType ID => SectionType.GameState;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct GameStateData
    {
        public int round_id;
        public float round_time;
    };

    static double seconds_in_day = 60*60*24;

    public unsafe bool Read(byte* ptr, int Size, ref int Counter)
    {
        if (Size != sizeof(GameStateData)) return false;
        var gdat = Unsafe.AsRef<GameStateData>(ptr);
        Times.SyncSpess(seconds_in_day*gdat.round_id+gdat.round_time);
        return true;
    }

    public bool Write(MemoryStream stream, ref int Counter)
    {
        return false;
    }
}