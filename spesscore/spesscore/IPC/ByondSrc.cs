using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace spesscore.IPC;

static class ByondSrc
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct BasicPacket
    {
        public uint PacketID;
        public uint PayloadSize;
        public short Sections;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct BasicSectionHeader
    {
        public short SectionType;
        public uint SectionSize;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EventEntryHeader
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 36)]
        public string Src;
        public byte Args;
        public byte NameLength;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct EventSection
    {
        short EventCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct GameStateSection
    {
        public float GameTime;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct ToDestroy
    {
        public uint Count;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct CallbackReturn
    {
        public uint CallbackID;
        public byte Args;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct NetCreate
    {
        public uint Network;
        public ushort NumMembers;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct NetAdd
    {
        public uint Network;
        public ushort NumMembers;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct NetRemove
    {
        public uint Network;
        public ushort NumMembers;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct NetDestroy
    {
        public uint Network;
    }

    unsafe static bool ReadStruct<T>(Socket sock, out T dat) where T : unmanaged
    {
        byte[] buf = new byte[sizeof(T)];
        if (sock.Receive(buf) != buf.Length) {
            dat = default;
            return false;
        }
        fixed (byte* ptr = buf)
        {
            dat = Unsafe.AsRef<T>(ptr);
            return true;
        }
    }

    public static bool ProcessPacket(Socket sock)
    {
        if (!ReadStruct(sock, out BasicPacket hdr)) return false;
        for (int i = 0; i < hdr.Sections; i++)
        {
            if (!ReadStruct(sock, out BasicSectionHeader sechdr)) return false;
            switch ((SectionType)sechdr.SectionType)
            {
                case SectionType.Ping:
                    // reply with pong immediately
                    break;
                case SectionType.Event:
                    // push events
                    break;
                case SectionType.GameState:
                    // update game state
                    break;
            }
        }
        //byte[] hdr_b = new byte[];
        //sock.Receive(hdr_b);
        return true;
    }
}