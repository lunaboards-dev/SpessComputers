#include "ipc.hpp"



BYOND_API_METHOD(ipc_pump) {
    if (argc < 1) return ByondFalse;
    return IPC_Next(argv) ? ByondTrue : ByondFalse;
}