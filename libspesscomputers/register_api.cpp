#include "ipc.hpp"

struct dev_info {
    
};

BYOND_API_METHOD(dev_register) {
    if (argc != 2) {
        Byond_CRASH("dev_register needs src and dev");
    }

    CByondValue * ss = argv;
    CByondValue * dev = argv+1;

    std::vector<char> buffer;

    IPC_Send(ss, IPC_SRegisterDev, )
}