namespace spesscore.IPC.RPC;

class RPCMethod
{
    int ObjRef;
    int ProcRef;
    public string MethodName;
    public RPCArgType ReturnValue;
    public RPCArgType[] ArgTypes;

    public void MakeCall(int dref, int compid, List<object> args)
    {
        Stream str = new MemoryStream();
        BinaryWriter br = new BinaryWriter(str);
    }
}