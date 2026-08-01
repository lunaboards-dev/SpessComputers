using spesscore.VM.Libraries;
using spesscore.VM.Peripheral;
using spesscore.VM.Peripheral.Disk;

namespace spesscore.VM;

class Computer
{
    public VMCore VM;
    public TTY? LocalTerminal;
    public List<IPeripheral> Peripherals;

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
        //VM.AddLibrary(new ComputerLib(this));
    }

    public void AddPeripheral(IPeripheral per)
    {
        Peripherals.Add(per);
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
        
    }

    public bool TryResume()
    {
        return true;
    }
}