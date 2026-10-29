namespace spesscore.IPC.RPC;

class RPCMap
{
    Dictionary<RPCItem, RPCItem> Pairs = new();

    public unsafe byte[] Serialize()
    {
        List<byte> types = [];
        MemoryStream str = new();
        uint idx = 0;
        foreach (var pair in Pairs)
        {
            byte[] key = pair.Key.Serialize();
            byte[] value = pair.Value.Serialize();
            types.Add((byte)pair.Key.Type);
            types.Add((byte)pair.Value.Type);
            str.Write(key);
            str.Write(value);
            idx += 2;
        }
        byte[] buffer = new byte[types.Count+str.Position+4];
        fixed(byte * ptr = buffer) ((int*) ptr)[0] = (int)(types.Count+str.Position);
        types.CopyTo(buffer, 4);
        Array.Copy(str.GetBuffer(), types.Count+4, buffer, 0, str.Position);
        str.Close();
        return buffer;
    }
}