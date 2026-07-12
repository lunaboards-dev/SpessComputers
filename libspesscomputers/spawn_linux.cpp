#ifdef __linux__
#include "spawn.hpp"
#include <spawn.h>
#include <stdlib.h>
#include <unistd.h>
#include <strings.h>
#include <string.h>
#include <sys/prctl.h>
#include <sys/wait.h>

int OpenSpesscore(std::string path, std::vector<std::string> args, int * spess_pid) {
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
    auto pid = fork();
    if (pid == 0) {
        //int ok = posix_spawn(&pid, path.c_str(), nullptr, nullptr, cargs, environ);
        if (prctl(PR_SET_PDEATHSIG, SIGHUP) != 0) {
            fprintf(stderr, "prctl failure");
            _exit(1);
        }
        execv(path.c_str(), cargs);
        fprintf(stderr, "execv");
        _exit(1);
    } else if (pid > 0) {
        while (*carg_ptr != NULL) {
            sc_free(*carg_ptr++);
        }
        sc_free(cargs);
        *spess_pid = pid;
        return 0;
    }
    return 1;
}

#endif