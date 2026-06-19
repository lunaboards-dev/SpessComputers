
namespace spesscore.VM.Components;

class DMComponent : IComponent
{
    public Dictionary<string, IComponent.ComponentCallback> Callbacks => throw new NotImplementedException();

    public string ComponentName => throw new NotImplementedException();

    public string ID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public void Attach(Computer computer)
    {
        throw new NotImplementedException();
    }

    public void Detach(Computer computer)
    {
        throw new NotImplementedException();
    }
}