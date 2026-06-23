
namespace spesscore.VM.Peripheral;

abstract class AbstractPeripheral(string name) : IPeripheral
{
    string id = "";

    public string PeripheralName => name;

    public string ID
    {
        get => id;
        set
        {
            SetID(value);
            id = value;
        }
    }

    public Computer? Computer => Host;

    public abstract Dictionary<string, IPeripheral.PeripheralCallback> Callbacks { get; }

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