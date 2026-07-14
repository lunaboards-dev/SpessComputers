#include "core.hpp"
#include "spawn.hpp"
#include "ipc.hpp"

#include <string>
#include <vector>
#include <stdarg.h>
#include <string.h>
#include <sys/socket.h>
#include <sys/un.h>
#include <ctime>
#include <format>

#ifdef __linux__
#include <unistd.h>
#define stricmp strcasecmp
#elifdef __WIN32__
#include <windows.h>
#define stricmp _stricmp
#define getpid GetCurrentProcessId
#endif

SpessComputers Core;

void bwoink(CByondValue &src, const char * msg) {
    printf("\x1b[31mSPESSCOMPUTERS ERROR: %s\x1b[0m\n", msg);
    CByondValue str;
    ByondValue_SetStr(&str, msg);
    CByondValue vlist;
    Byond_CreateListLen(&vlist, 1);
    Byond_WriteList(&vlist, &str, 1);
    CByondValue Res; // discarded
    Byond_CallProc(&src, "BwoinkatizeMeCaptain", &vlist, 1, &Res);
}

// automagically allocates a string for formatting
int auto_vsprintf(char ** ptr, const char * fmt, va_list args) {
    int needed = vsnprintf(NULL, 0, fmt, args); // get the size of buffer we need
    *ptr = (char*) sc_alloc(needed); // this needs to be freed when you're done
    vsprintf(*ptr, fmt, args);
    return needed;
}

// see above, but we use varargs
int auto_sprintf(char ** ptr, const char * fmt, ...) {
    va_list args;

    va_start(args, fmt);
    int rtv = auto_vsprintf(ptr, fmt, args);
    va_end(args);

    return rtv;
}

// automagically allocate string from byond string
char * auto_b2str(CByondValue& v) {
    u4c len = 0;
    Byond_ToString(&v, NULL, &len); // get length of byond string
    char * buffer = (char*) sc_alloc(len);
    Byond_ToString(&v, buffer, &len);
    return buffer;
}

/* void super_free(std::vector<char *> args) {
    auto ptr = args.cbegin();
    while (ptr < args.cend()) {
        free(*ptr++);
    }
} */

std::vector<std::string> CreateArgList(CByondValue &val, std::string &workspace_path, std::string &exec_path, std::string &ipc_path) {
    u4c len = 0;
    Byond_ReadListAssoc(&val, NULL, &len);
    std::vector<CByondValue> values(len);
    //CByondValue values[len];
    Byond_ReadListAssoc(&val, values.data(), &len);
    std::vector<std::string> args;
    for (int i = 0; i < len; i+=2) {
        char *kval = auto_b2str(values[i]);
        char *vval = auto_b2str(values[i+1]);
        printf("config: %s=%s\n", kval, vval);
        if (stricmp(kval, "execpath") == 0) {
            exec_path = vval;
            free(kval);
            free(vval);
            continue;
        } else if (stricmp(kval, "workspacepath") == 0) {
            workspace_path = vval;
        } else if (stricmp(kval, "ipcsocketpath") == 0) {
            ipc_path = vval;
        }
        //auto_sprintf(&buf, "%s=%s", kval, vval);
        std::string buf = std::format("{}={}", kval, vval); // vsc says this doesn't exist btw
        free(kval);
        free(vval);
        args.push_back(buf);
    }
    args.push_back(std::format("parentpid={}", getpid()));
    args.insert(args.begin(), exec_path);
    if (ipc_path[0] == '.' && ipc_path[1] == '/') {
        ipc_path = workspace_path + "/" + ipc_path;
    }
    return args;
}

struct sc_state {
    bool running;
    bool crashed;
    int code;
};

#ifdef __linux__
#include <sys/wait.h>
#include <signal.h>
#include <stdexcept>
sc_state proc_state(int pid) {
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

#elifdef __WIN32__
sc_state proc_state(int pid) {

}
#endif

bool is_proc_kill(int pid, CByondValue &src) {
    auto st = proc_state(pid);
    if (st.running) return false;
    if (st.crashed) { // everything is fucked
        bwoink(src, std::format("spesscore crashed! ({})", strsignal(st.code)));
        return true;
    } else { // what? probably an uncaught C# exception, though this should SIGABRT
        bwoink(src, std::format("spesscore stopped! (exit code: {})", st.code));
        return true;
    }
    return false;
}

SpessComputers::~SpessComputers() {
    // clean up our mess
}

bool AttemptRecovery(CByondValue &src) {
    if (proc_state(Core.PID).running) { // spesscore is still valid! why did we restart?
        WTF_BWOINK(src, "attempt to restart despite core still running!");
        return false;
    }
    auto old = Core;
    Core = {};
    // idk, recover or something
    Core.Valid = true;
    // pump errors from old state
    for (auto s : old.StoredBwoinks) {
        bwoink(src, s);
    }
    // Peripherals, networks, and computers are now invalid.
    // TODO: we should attempt to recover these
    // recover terminals
    for (auto term : old.Terminals) {
        term.second.id = "";
        term.second.status = TTY_STAT_RECOVER | TTY_STAT_NEED_ID;
        Core.TTYCreateRequests.push(term.second);
    }
    old.Terminals.clear();
    return true;
}

BYOND_API_METHOD(init) {
    try {
        if (argc < 2) return ByondFalse;
        auto src = argv[0];
        auto args = argv[1];
        if (Core.Valid) {
            if (!AttemptRecovery(src)) {
                Byond_CRASH("i should kick your fucking ass");
                return ByondFalse;
            }
        } else {
            Core = {}; // clear out our core lol
        }
        std::string ws_path = "";
        std::string exe_path = "";
        std::string sock_path = "";

        auto vars = CreateArgList(args, ws_path, exe_path, sock_path);
        int pid = 0;
        int ok = OpenSpesscore(exe_path, vars, &pid);
        if (ok != 0) {
            char * ebuf = nullptr;
            auto_sprintf(&ebuf, "failed to start spesscore: %s (%d)", strerror(ok), ok);
            bwoink(src, ebuf); // thousands of spessmen must die
            return ByondFalse;
        }
        Core.PID = pid;
        struct sockaddr_un addr;
        memset(&addr, 0, sizeof(addr));
        addr.sun_family = AF_UNIX;
        if (sock_path.length() > sizeof(addr.sun_path)-1) {
            bwoink(src, "ipc path too long!");
        }
        strcpy(addr.sun_path, sock_path.c_str());
        Core.SocketAddr = addr;
        return ByondTrue;
    } catch (const std::runtime_error& e) {
        bwoink(argv[0], std::format("c++ exception: {}", e.what()).c_str());
        return ByondFalse;
    } catch (...) {
        return ByondFalse;
    }
}

inline CByondValue IntVal(int v) {
    CByondValue bv = {
        .type = NUMBER,
        .data = {
            .num = v
        }
    };
    return bv;
}

#define SC_CON_OK 0
#define SC_CON_RETRY 1
#define SC_CON_DEAD 2

BYOND_API_METHOD(init_try_connect) {
    try {
        int fd;
        if (argc < 1) return IntVal(SC_CON_DEAD);
        if (is_proc_kill(Core.PID, *argv)) {
            return IntVal(SC_CON_DEAD);
        }
        if ((fd = socket(PF_UNIX, SOCK_STREAM, 0)) < 0) {
            // bwoink
            bwoink(argv[0], "failed to create socket");
            return IntVal(SC_CON_DEAD);
        }
        if (connect(fd, (struct sockaddr *) &Core.SocketAddr, sizeof(Core.SocketAddr)) == -1) {
            // bwoink
            close(fd);
        } else {
            Core.Handle = fd;
            return IntVal(SC_CON_OK);
        }
        return IntVal(SC_CON_RETRY);
    } catch (const std::runtime_error& e) {
        bwoink(argv[0], std::format("c++ exception: {}", e.what()).c_str());
        return IntVal(SC_CON_DEAD);
    } catch (...) {
        return IntVal(SC_CON_DEAD);
    }
}

BYOND_API_METHOD(tick) {
    if (argc < 1) return ByondFalse;
    try {
        // housekeeping
        IPC_Flush(argv);
    } catch (const std::runtime_error& e) {
        bwoink(argv[0], std::format("c++ exception: {}", e.what()).c_str());
        return ByondFalse;
    } catch (...) {
        return ByondFalse;
    }
    return ByondTrue;
}