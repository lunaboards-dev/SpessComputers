#pragma once
#include "debug.hpp"
#include <vector>
#include <string>
#include <unordered_map>
#include <byondapi.h>
#include <sys/socket.h>
#include <sys/un.h>
#include <format>
#include <queue>

#define NUMBER 0x2A
#define DATUM 0x21

static CByondValue ByondTrue = {
    .type = NUMBER,
    .data = {.num = 1}
};

static CByondValue ByondFalse = {
    .type = NUMBER,
    .data = {.num = 0}
};

#define TTY_STAT_OK 0
#define TTY_STAT_UNOWNED 1
#define TTY_STAT_NEED_ID 2
#define TTY_STAT_KILLED 3
#define TTY_STAT_RECOVER (1 << 15)
#define TTY_STAT_MASK 0x7FFF

struct TTY {
    CByondValue ref;
    std::string id;
    unsigned short status;
};

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
    bool Valid;
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
    std::string WsPath;
    std::string ExePath;
    std::string IpcPath;
    sockaddr_un SocketAddr;
    int Handle;
    int PID;
    std::queue<TTY> TTYCreateRequests;
    std::unordered_map<u4c, TTY> Terminals;
    ~SpessComputers();
};

extern SpessComputers Core;

#define BYOND_API_METHOD(method) CByondValue spess_##method(int argc, CByondValue * argv)
#define BYOND_API_DEF(method) extern "C" BYOND_EXPORT BYOND_API_METHOD(method);

BYOND_API_DEF(init)
BYOND_API_DEF(init_try_connect)
BYOND_API_DEF(tick)
BYOND_API_DEF(power)
BYOND_API_DEF(send_signal)
BYOND_API_DEF(register_api)

void bwoink(CByondValue &src, const char * msg);
inline void bwoink(CByondValue &src, std::string msg) {
    bwoink(src, msg.c_str());
}

#define WTF_BWOINK(SSsc, msg) bwoink(SSsc, std::format("WTF!? {}({}:{}) - {}", __func__, __FILE__, __LINE__, msg))