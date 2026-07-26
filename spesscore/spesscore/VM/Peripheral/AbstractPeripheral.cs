
using System.Runtime.InteropServices;
using System.Text;

namespace spesscore.VM.Peripheral;

abstract class AbstractPeripheral(string name) : IPeripheral
{
    string id = "";

    public string PeripheralName => name;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct IDUpdate
    {
        unsafe public IDUpdate(uint rid, string id)
        {
            refid = rid;
            fixed (byte * ptr = this.id)
            {
                Span<byte> span = new(ptr, 37);
                Encoding.ASCII.GetBytes(id).CopyTo(span);
                ptr[36] = 0;
            }
        }
        public uint refid;
        unsafe public fixed byte id[37];
        public byte pad;
    }
    unsafe static void SendIDUpdate(uint refid, string id)
    {
        var dat = new IDUpdate(refid, id);
        SpessCore.Instance.IPC.Send(IPC.SectionType.SetID, (byte*)&dat, sizeof(IDUpdate));
    }

    public string ID
    {
        get => id;
        set
        {
            SetID(value);
            id = value;
            if (GetRef() > 0)
            {
                SendIDUpdate(GetRef(), value);
            }
        }
    }

    

    public virtual uint GetRef()
    {
        return 0;
    }

    public Computer? Computer => Host;

    public abstract Dictionary<string, IPeripheral.PeripheralCallback> Callbacks { get; }

    public uint Reference => GetRef();

    public virtual void SetID(string _id)
    {
        
    }

    internal Computer? Host;
    
    public void Attach(Computer com)
    {
        Host = com;
    }

    public void Detach(Computer com)
    {
        Host = null;
    }

    public abstract void Destroy();
}