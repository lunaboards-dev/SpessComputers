namespace spesscore.IPC.RPC;

enum RPCArgType : byte
{
    Undefined,
    Int,
    Float,
    Bool,
    String,
    Blob,
    List,
    Map,
    Invalid,
    Null = 128
}