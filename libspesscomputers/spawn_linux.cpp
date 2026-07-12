#ifdef __linux__
#include "spawn.hpp"
#include <spawn.h>
#include <stdlib.h>
#include <unistd.h>
#include <strings.h>

int OpenSpesscore(std::string path, std::vector<std::string> args, int * spess_pid) {
    pid_t pid = 0;
    std::vector<const char *> cargs;
    for (std::string s : args) {
        cargs.push_back(s.c_str());
    }
    int ok = posix_spawn(&pid, path.c_str(), nullptr, nullptr, (char**)cargs.data(), environ);
    *spess_pid = pid;
    return ok;
}

#endif