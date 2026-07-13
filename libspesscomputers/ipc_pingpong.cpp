#include "ipc.hpp"
#include <chrono>

bool PingPongWrite(std::stringstream stream, int * ctr) {
    if (*ctr > 0) return false;
    *ctr++;
    auto cnow = std::chrono::system_clock::now();
    auto duration = cnow.time_since_epoch();
    // this better be a fucking double or i'll kill john gnu
    double ns = std::chrono::duration<double, std::milli>(duration).count();

    stream.write((const char*)&ns, sizeof(double));
    return true;
}