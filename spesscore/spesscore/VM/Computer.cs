using System.Runtime.InteropServices;
using KeraLua;

namespace spesscore.VM;

class Computer
{
    int max_memory;
    int cpu_speed;
    int currently_allocated;

    Lua L;

    unsafe nint Allocator(nint ud, nint ptr, nuint osize, nuint nsize)
    {
        nint delta = ((int)nsize)-((int)osize);
        if (delta+currently_allocated > max_memory)
        {
            return 0; // wrong, chlorine trifluoride
        }
        void* p = NativeMemory.Realloc((void*)ptr, nsize);
        currently_allocated+=(int)delta;
        return (nint)p;
    }

    void InitLuaState()
    {
        L = new Lua(Allocator, 0);
    }

    void MemoryResize(int newsize)
    {
        if (currently_allocated > newsize)
        {
            L.Error("Out of memory");
        }
    }

    // STAHP! NO!
    void Stop()
    {
        
    }

    void Start()
    {
        
    }

    void Pause()
    {
        
    }
}