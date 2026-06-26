using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace spesscore.IPC;

static class ByondSrc
{
    [StructLayout(LayoutKind.Sequential)]
    struct BasicPacket
    {
        public uint PacketID;
        public uint PayloadSize;
        public short Sections;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct BasicSectionHeader
    {
        public short SectionType;
        public uint SectionSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct EventEntryHeader
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 36)]
        public string Src;
        public byte Args;
        public byte NameLength;
    };

    [StructLayout(LayoutKind.Sequential)]
    struct EventSection
    {
        short EventCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct GameStateSection
    {
        public float GameTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ToDestroy
    {
        public uint Count;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct CallbackReturn
    {
        public uint CallbackID;
        public byte Args;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct NetCreate
    {
        public uint Network;
        public ushort NumMembers;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct NetAdd
    {
        public uint Network;
        public ushort NumMembers;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct NetRemove
    {
        public uint Network;
        public ushort NumMembers;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct NetDestroy
    {
        public uint Network;
    }

    public static void ProcessPacket(Socket sock)
    {
        
    }
}