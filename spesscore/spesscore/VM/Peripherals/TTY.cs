
using static spesscore.VM.Lua;
using static spesscore.VM.Helpers;
using spesscore.Terminal;
using System.Text;

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
            {"buffer_size", BufferSize},
            {"read", Read},
            {"next", NextInput}
        };
    }

    public override void SetID(string id)
    {
        listener = SpessCore.Instance?.TServ.NewListener(id); // i will be impressed if this is ever null
        listener.OnInput += FillBuffer;
    }

    public void FillBuffer(byte[] bytes)
    {
        buffer.Write(bytes);
        Console.WriteLine($"bytes: {bytes.Length}, buffer size: {buffer.Count}");
    }

    public void Write(string str)
    {
        listener?.Write(Encoding.UTF8.GetBytes(str));
    }

    int Read(lua_State L)
    {
        long amt = luaL_checkinteger(L, 2);
        byte[] dat = buffer.Read((uint)amt);
        lua_pushbytebuffer(L, dat);
        return 1;
    }

    int Write(lua_State L)
    {
        byte[] str = luaL_checkbytebuffer(L, 2);
        listener?.Write(str);
        return 0;
    }

    int BufferSize(lua_State L)
    {
        lua_pushinteger(L, buffer.Count);
        return 1;
    }

    int NextInput(lua_State L)
    {
        List<byte> rbuf = [];
        byte? fb = buffer.Next();
        if (fb == null) return 0;
        if (fb != 27)
        {
            lua_pushbytebuffer(L, [fb.Value]);
        }
        byte? b;
        while ((b = buffer.Next()) != null)
        {
            byte bv = b.Value;
            rbuf.Add(bv);
            char cv = (char)bv;
            if (char.IsAsciiLetter(cv))
                break;
        }
        lua_pushbytebuffer(L, rbuf.ToArray());
        return 1;
    }

    override public void Destroy()
    {
        SpessCore.Instance?.TServ.Remove(ID);
    }
}