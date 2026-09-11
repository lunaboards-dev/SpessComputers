#include <sys/wait.h>
#include <signal.h>
#include <stdexcept>
#include "spesscore.hpp"
#include "debug.hpp"
#include <sys/prctl.h>

class SpesscoreLinux : public SpessCore {
    int pid = 0;
    public:
        bool spawn(std::string path, std::vector<std::string> args) override {
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
                this->pid = pid;
                return 0;
            }
            return 1;
        }

        sc_state state() override {
            sc_state st = {
                .running = false,
                .crashed = false,
                .code = -1
            };
            int stat = 0;
            if (waitpid(pid, &stat, WNOHANG) == 0) {
                st.running = true;
            } else if (WIFSIGNALED(stat)) { // crashed
                st.running = false;
                st.crashed = true;
                st.code = WTERMSIG(stat);
            } else if (WIFEXITED(stat)) {
                st.running = false;
                st.crashed = false;
                st.code = WEXITSTATUS(stat);
            }
            return st;
        }
};

SpesscoreLinux sc;
SpessCore * Spesscore = &sc;