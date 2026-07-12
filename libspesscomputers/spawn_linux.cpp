#ifdef __linux__
#include "spawn.hpp"
#include <spawn.h>
#include <stdlib.h>
#include <unistd.h>
#include <strings.h>
#include <string.h>

int OpenSpesscore(std::string path, std::vector<std::string> args, int * spess_pid) {
    pid_t pid = 0;
    //std::vector<const char *> cargs;
    char** cargs = (char**) sc_alloc(sizeof(char*)*(args.size()+1)); // i'm going to explode
    auto carg_ptr = cargs;
    for (std::string s : args) {
        *carg_ptr = (char*)sc_alloc(s.length()+1);
        strcpy(*carg_ptr, s.c_str());
        carg_ptr++;
    }
    cargs[args.size()] = NULL; // make damn sure this is correct
    carg_ptr = cargs;
    int ok = posix_spawn(&pid, path.c_str(), nullptr, nullptr, cargs, environ);
    while (*carg_ptr != NULL) {
        sc_free(*carg_ptr++);
    }
    sc_free(cargs);
    *spess_pid = pid;
    return ok;
}

#endif