#pragma once
#include "core.hpp"
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
    IPC_SSetID,
    IPC_SCreateComputer
};

using IPCSectionReader = bool(*)(CByondValue&,void*,size_t);
using IPCSectionWriter = bool(*)(CByondValue&,std::stringstream&,int*);

struct IPCSectionHandler {
    IPCSectionID sec_id;
    IPCSectionReader read;
    IPCSectionWriter write;
};

extern IPCSectionHandler PingHandler;
extern IPCSectionHandler PongHandler;
extern IPCSectionHandler EventHandler;
extern IPCSectionHandler TTYHandler;
extern IPCSectionHandler SetIDHandler;

static IPCSectionHandler* Handlers[] = {
    &TTYHandler,
    &SetIDHandler,
    /* &PingHandler,
    &PongHandler,
    &EventHandler, */
    nullptr
};

bool IPC_Next(CByondValue * ss);
void IPC_Flush(CByondValue * ss);
bool IPC_Send(CByondValue * ss, IPCSectionID id, void * buffer, size_t size);

static bool NullWrite(CByondValue& ss, std::stringstream& stream,int*ctr) {
    return false;
}

static bool NullRead(CByondValue&,void*,size_t) {
    return false;
}

BYOND_API_DEF(ipc_pump);