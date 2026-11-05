using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using spesscore.IPC.Sections;

namespace spesscore.IPC;

class IPC
{
    Socket sock;
    Stream debug;
    Dictionary<SectionType, IIPCSection> Sections = [];
    Dictionary<SectionType, int> Counters = [];

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct SectionHeader
    {
        public short SectionType;
        public uint SectionSize;
    }

    public IPC(Socket s)
    {
        sock = s;
        AddSection(new CreateTTY()); // fuck it, this is all we need
        AddSection(new DevRegister());
        debug = File.OpenWrite("debug.bin");
    }

    void hint(bool recv)
    {
        debug.WriteByte((byte)(recv ? 0 : 1));
    }

    int recv(byte[] buffer, bool soh = false)
    {
        int amt = sock.Receive(buffer);
        if (soh) hint(true);
        debug.Write(buffer, 0, amt);
        debug.Flush();
        return amt;
    }

    int send(byte[] buffer, bool soh = false)
    {
        int amt = sock.Send(buffer);
        if (soh) hint(false);
        debug.Write(buffer);
        debug.Flush();
        return amt;
    }

    int send(ReadOnlySpan<byte> buffer, bool soh = false)
    {
        int amt = sock.Send(buffer);
        if (soh) hint(false);
        debug.Write(buffer);
        return amt;
    }

    unsafe public bool Next()
    {
        byte[] hdr_buf = new byte[sizeof(SectionHeader)];
        int amt = recv(hdr_buf, true);
        if (amt != hdr_buf.Length)
        {
            // cry about it
            SpessCore.Instance.Bwoink("got incomplete header");
            return false;
        }
        fixed (byte * hptr = hdr_buf)
        {
            SectionHeader * hdr = (SectionHeader*) hptr;
            Console.WriteLine($"(DEBUG) IPC: Section type: {hdr->SectionType}, Size: {hdr->SectionSize}");
            byte[] body = new byte[hdr->SectionSize];
            amt = recv(body);
            if (Sections.TryGetValue((SectionType)hdr->SectionType, out IIPCSection? sec)) {
                fixed (byte * ptr = body)
                {
                    int ctr = 0;
                    sec.Read(ptr, (int)hdr->SectionSize, ref ctr);
                }
                return (SectionType)hdr->SectionType == SectionType.GameState;
            } else
            {
                SpessCore.Instance.Bwoink("unknown section type: "+hdr->SectionType);
            }
        }
        return false;
    }

    unsafe public void Flush()
    {
        foreach (var sec in Sections)
        {
            int ctr = 0;
            while (true)
            {
                MemoryStream str = new();
                bool ok = sec.Value.Write(str, ref ctr);
                if (ok)
                {
                    SectionHeader header = new()
                    {
                        SectionSize = (uint)str.Length,
                        SectionType = (short)sec.Value.ID
                    };
                    byte * ptr = (byte*)&header;
                    ReadOnlySpan<byte> bytes = new(ptr, sizeof(SectionHeader));
                    send(bytes, true);
                    send(str.GetBuffer());
                } else break;
            }
        }
    }

    unsafe public void Send(SectionType sec, byte * ptr, int size)
    {
        var data = new ReadOnlySpan<byte>(ptr, size);
        SectionHeader header = new()
        {
            SectionSize = (uint)size,
            SectionType = (short)sec
        };
        var hdr = new ReadOnlySpan<byte>((byte*)&header, sizeof(SectionHeader));
        send(hdr, true);
        send(data);
    }

    void AddSection(IIPCSection sec)
    {
        Sections[sec.ID] = sec;
    }
}