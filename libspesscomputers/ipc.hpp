#pragma once
#include <cstdint>
#include <cstddef>
#include <sstream>
#include <byondapi.h>

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
    IPC_STapeDataQuery,
    IPC_SHolocardRead,
    IPC_SHolocardWrite,
    IPC_SComputerPower,
    IPC_SGenericPeripheralCall,
    IPC_SCreateTTY,
    IPC_SCreateComputer
};

using IPCSectionReader = bool(*)(CByondValue&,void*,size_t);
using IPCSectionWriter = bool(*)(CByondValue&,std::stringstream);

struct IPCSectionHandler {
    IPCSectionID sec_id;
    IPCSectionReader read;
    IPCSectionWriter write;
};

extern IPCSectionHandler PingHandler;
extern IPCSectionHandler PongHandler;
extern IPCSectionHandler EventHandler;
extern IPCSectionHandler TTYHandler;

static IPCSectionHandler* Handlers[] = {
    &TTYHandler,
    /* &PingHandler,
    &PongHandler,
    &EventHandler, */
    nullptr
};