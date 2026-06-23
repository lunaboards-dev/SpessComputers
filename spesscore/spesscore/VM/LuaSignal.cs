using System.Runtime.InteropServices;
using KeraLua;

namespace spesscore.VM;

struct LuaSignal
{
    public string Name;
    public string Sender;
    public object[] args;

    public int Push(Lua L)
    {
        L.PushString(Name);
        L.PushString(Sender);
        foreach (object o in args)
        {
            if (o is string str)
            {
                L.PushString(str);
            } else if (o is long ival)
            {
                L.PushInteger(ival);
            } else if (o is double dval)
            {
                L.PushNumber(dval);
            } else if (o is bool bval)
            {
                L.PushBoolean(bval);
            } else
            {
                L.PushNil();
            }
        }
        return args.Length+2;
    }
}