using System.Diagnostics;

namespace spesscore;
static class Times
{
    static Stopwatch stop = Stopwatch.StartNew();
    public static double CurTime => stop.Elapsed.TotalSeconds;

    static double LastSync = 0;
    static double LastSpess = 0;
    public static double SpessTime => LastSpess + (CurTime-LastSync);

    public static void SyncSpess(double spess)
    {
        LastSpess = spess;
        LastSync = CurTime;
    }
}