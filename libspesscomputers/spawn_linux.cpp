#ifdef __linux__
#include "spawn.hpp"
#include <spawn.h>
#include <stdlib.h>
#include <unistd.h>
#include <strings.h>

int OpenSpesscore(std::string path, std::vector<char *> args, int * spess_pid) {
    pid_t pid = 0;
    int ok = posix_spawn(&pid, path.c_str(), nullptr, nullptr, args.data(), environ);
    *spess_pid = pid;
    return ok;
}

#endif