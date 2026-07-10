#ifdef __WIN32__
#error "Windows process spawning not yet implemented."
#include "spawn.hpp"

int OpenSpesscore(std::string path, std::vector<char *> args, int * spess_pid) {
    return 0;
}

#endif