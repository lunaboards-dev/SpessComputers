
using KeraLua;
using spesscore.Terminal;

namespace spesscore.VM.Peripheral;

class TTY : AbstractPeripheral
{
    override public Dictionary<string, IPeripheral.PeripheralCallback> Callbacks => callbacks;
    RingBuffer<byte> buffer = new(1024); // if you don't process 1KiB of inputs in time, that's on you.
    TerminalListener? listener;

    Dictionary<string, IPeripheral.PeripheralCallback> callbacks;
    public TTY() : base("tty")
    {
        callbacks = new()
        {
            {"write", Write},
            {"buffer_size", BufferSize}
        };
    }

    public override void SetID(string id)
    {
        listener = SpessCore.Instance?.TServ.NewListener(id); // i will be impressed if this is ever null
    }

    public void FillBuffer(byte[] bytes)
    {
        buffer.Write(bytes);
    }

    int Write(Lua L)
    {
        byte[] str = L.CheckBuffer(2);
        listener?.Write(str);
        return 0;
    }

    int BufferSize(Lua L)
    {
        L.PushInteger(buffer.Count);
        return 1;
    }

    override public void Destroy()
    {
        SpessCore.Instance?.TServ.Remove(ID);
    }
}