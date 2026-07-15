
using static spesscore.VM.Lua;
using static spesscore.VM.Helpers;
using spesscore.Terminal;
using System.Text;
using Cyotek.Collections.Generic;

namespace spesscore.VM.Peripheral;

class TTY : AbstractPeripheral
{
    override public Dictionary<string, IPeripheral.PeripheralCallback> Callbacks => callbacks;
    CircularBuffer<byte> buffer = new(1024, true); // if you don't process 1KiB of inputs in time, that's on you.
    TerminalListener? listener;
    uint href;

    public override uint GetRef()
    {
        return href;
    }

    Dictionary<string, IPeripheral.PeripheralCallback> callbacks;
    public TTY(uint holder) : base("tty")
    {
        callbacks = new()
        {
            {"write", Write},
            {"buffer_size", BufferSize},
            {"read", Read},
            {"next", NextInput}
        };
        href = holder;
    }

    public override void SetID(string id)
    {
        listener = SpessCore.Instance.TServ.NewListener(id);
        listener.OnInput += FillBuffer;
        // Fire Set ID event
    }

    public void FillBuffer(byte[] bytes)
    {
        buffer.Put(bytes);
        Console.WriteLine($"bytes: {bytes.Length}, buffer size: {buffer.Size}");
    }

    public void Write(string str)
    {
        listener?.Write(Encoding.UTF8.GetBytes(str));
    }

    int Read(lua_State L)
    {
        long amt = luaL_checkinteger(L, 2);
        byte[] dat = buffer.Get((int)amt);
        lua_pushbytebuffer(L, dat);
        return 1;
    }

    int Write(lua_State L)
    {
        byte[] str = luaL_checkbytebuffer(L, 2);
        if (str.Length == 0) return 0;
        listener?.Write(str);
        return 0;
    }

    int BufferSize(lua_State L)
    {
        lua_pushinteger(L, buffer.Size);
        return 1;
    }

    int NextInput(lua_State L)
    {
        List<byte> rbuf = [];
        if (buffer.IsEmpty) return 0;
        byte fb = buffer.Get();
        if (fb != 27)
        {
            lua_pushbytebuffer(L, [fb]);
            return 1;
        }
        rbuf.Add(fb);
        while (!buffer.IsEmpty)
        {
            byte bv = buffer.Get();
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