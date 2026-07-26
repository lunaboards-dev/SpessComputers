
namespace spesscore.VM.Peripheral;

class DMPeripheral : IPeripheral
{
    public Dictionary<string, IPeripheral.PeripheralCallback> Callbacks => throw new NotImplementedException();

    public string PeripheralName => throw new NotImplementedException();

    public string ID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public Computer? Computer => throw new NotImplementedException();

    public uint Reference => throw new NotImplementedException();

    public void Attach(Computer computer)
    {
        throw new NotImplementedException();
    }

    public void Destroy()
    {
        // this is never called, as it would handled DM-side.
    }

    public void Detach(Computer computer)
    {
        throw new NotImplementedException();
    }
}