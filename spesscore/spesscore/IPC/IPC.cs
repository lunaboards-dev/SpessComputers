using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using spesscore.IPC.Sections;

namespace spesscore.IPC;

class IPC
{
    Socket sock;
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
    }

    unsafe public bool Next()
    {
        byte[] hdr_buf = new byte[sizeof(SectionHeader)];
        int amt = sock.Receive(hdr_buf);
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
            amt = sock.Receive(body);
            if (Sections.TryGetValue((SectionType)hdr->SectionType, out IIPCSection sec)) {
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
                    sock.Send(bytes);
                    sock.Send(str.GetBuffer());
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
        sock.Send(hdr);
        sock.Send(data);
    }

    void AddSection(IIPCSection sec)
    {
        Sections[sec.ID] = sec;
    }
}