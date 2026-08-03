using spesscore.VM.Libraries;
using spesscore.VM.Peripheral;
using spesscore.VM.Peripheral.Disk;

namespace spesscore.VM;

class Computer
{
    public VMCore VM;
    public TTY? LocalTerminal => LocalTTY;
    public List<IPeripheral> Peripherals = [];

    public TTY? LocalTTY;
    public ManagedDisk? Disk;
    public EEPROM? BIOS;

    public int MaxMemory
    {
        get => (int)VM.MaxMemory;
        set => VM.MaxMemory = value;
    }

    public Computer()
    {
        VM = new(SpessCore.Instance.MachineLua);
        VM.AddLibrary(new ComputerLib(this));
        VM.AddLibrary(new PeripheralAPI(this));
    }

    public void AddPeripheral(IPeripheral per)
    {
        Peripherals.Add(per);
        per.Attach(this);
    }

    public IPeripheral? GetPeripheral(string id)
    {
        foreach (var per in Peripherals)
        {
            if (per.ID == id) return per;
        }
        return null;
    }

    public void Stop()
    {
        VM.Pause();
    }

    public void TogglePower(bool hard)
    {
        if (VM.Active)
        {
            if (hard)
            {
                VM.Stop();
            }
        } else
        {
            VM.Start();
        }
    }

    public bool TryResume()
    {
        return VM.Resume();
    }
}