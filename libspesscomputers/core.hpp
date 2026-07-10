#pragma once
#include <vector>
#include <string>
#include <unordered_map>
#include <byondapi.h>

struct NetworkUpdate {

};

struct ComputerState {
    std::vector<std::string> ConnectedPeripherals;
    std::string RefId;
    char PowerState; // 1 - Requested, 2 - On/off, 3 - hard
    bool Dirty;
};

// this is a big buffer of all the info we need
struct SpessComputers {
    std::vector<std::string> StoredBwoinks;
    bool ShouldPong;
    double PingTime;
    std::vector<void*> PeripheralCalls;
    std::vector<void*> PeripheralReturns;
    std::vector<std::string> DestroyedPeripherals;
    std::vector<int> DestroyedNetworks;
    std::vector<int> CreatedNetworks;
    std::vector<u4c> CreatedComputers;
    std::vector<std::string> DestroyedComptuers;
    std::unordered_map<u4c, ComputerState> ComputerMapping;
    int Handle;
};

extern SpessComputers Core;

#define BYOND_API_METHOD(method) CByondValue spess_##method(int argc, CByondValue * argv)
#define BYOND_API_DEF(method) extern "C" BYOND_EXPORT BYOND_API_METHOD(method);

BYOND_API_DEF(init)
BYOND_API_DEF(tick)
BYOND_API_DEF(power)
BYOND_API_DEF(send_signal)
BYOND_API_DEF(register_api)