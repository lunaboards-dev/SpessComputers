namespace spesscore.IPC;

    enum SectionType : short
    {
        Ping,
        Pong,
        Event,
        GameState,
        Destroy,
        Callback,
        NetCreate,
        NetAdd,
        NetRemove,
        NetDestroy,
        PeripheralCall,
        Bwoink,
        ManagedDiskQuery,
        TapeDataQuery,
        HolocardRead,
        HolocardWrite,
        ComputerPower,
        GenericPeripheralCall, // this will be jank as shit, might be useful though
        CreateTTY
    }