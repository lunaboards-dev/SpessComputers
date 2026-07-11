#pragma once
#include <cstdint>
#include <cstddef>
#include <sstream>

enum IPCSectionID : uint16_t {
    IPC_SPing,
    IPC_SPong,
    IPC_SEvent,
    IPC_SGameState,
    IPC_SDestroy,
    IPC_SCallback,
    IPC_SNetCreate,
    IPC_SNetAdd,
    IPC_SNetRemove,
    IPC_SNetDestroy,
    IPC_SPeripheralCall,
    IPC_SBwoink,
    IPC_SManagedDiskQuery,
    IPC_SManagedDiskReturn,
    IPC_STapeDataQuery,
    IPC_STapeDataReturn,
    IPC_SHolocardRead,
    IPC_SHolocardWrite,
    IPC_SHolocardReturnR,
    IPC_SHolocardReturnW,
    IPC_SComputerPower,
    IPC_SGenericPeripheralCall
};

using IPCSectionReader = bool(*)(void*,size_t,int*);
using IPCSectionWriter = bool(*)(std::stringstream,int*);

struct IPCSectionHandler {
    IPCSectionID sec_id;
    IPCSectionReader read;
    IPCSectionWriter write;
};

extern IPCSectionHandler PingHandler;
extern IPCSectionHandler PongHandler;
extern IPCSectionHandler EventHandler;

IPCSectionHandler* Handlers[] = {
    /* &PingHandler,
    &PongHandler,
    &EventHandler, */
    nullptr
};