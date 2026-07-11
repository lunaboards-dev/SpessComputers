#include "core.hpp"
#include "spawn.hpp"

#include <string>
#include <vector>
#include <stdarg.h>
#include <string.h>
#include <sys/socket.h>
#include <sys/un.h>
#include <ctime>

#ifdef __linux__
#define stricmp strcasecmp
#elifdef __WIN32__
#define stricmp _stricmp
#endif

SpessComputers Core;

void bwoink(CByondValue &src, const char * msg) {
    printf("\x1b[31mSPESSCOMPUTERS ERROR: %s\x1b[0m\n", msg);
    CByondValue bwoinks;
    Byond_ReadVar(&src, "bwoinks", &bwoinks);
    // sadness :(
    u4c len = 0;
    Byond_ReadList(&bwoinks, NULL, &len);
    CByondValue pos = {
        .type = NUMBER,
        .data {.num = len+1}
    };
    CByondValue str;
    ByondValue_SetStr(&str, msg);
    Byond_WriteListIndex(&bwoinks, &pos, &str);
}

int auto_vsprintf(char ** ptr, const char * fmt, va_list args) {
    int needed = vsnprintf(NULL, 0, fmt, args);
    *ptr = (char*) sc_alloc(needed);
    vsprintf(*ptr, fmt, args);
    return needed;
}

int auto_sprintf(char ** ptr, const char * fmt, ...) {
    va_list args;

    va_start(args, fmt);
    int rtv = auto_vsprintf(ptr, fmt, args);
    va_end(args);

    return rtv;
}

char * auto_b2str(CByondValue& v) {
    u4c len = 0;
    Byond_ToString(&v, NULL, &len);
    char * buffer = (char*) sc_alloc(len);
    Byond_ToString(&v, buffer, &len);
    return buffer;
}

void super_free(std::vector<char *> args) {
    auto ptr = args.cbegin();
    while (ptr < args.cend()) {
        free(*ptr++);
    }
}

std::vector<char *> CreateArgList(CByondValue &val, std::string &workspace_path, std::string &exec_path, std::string &ipc_path) {
    u4c len = 0;
    Byond_ReadListAssoc(&val, NULL, &len);
    CByondValue values[len];
    Byond_ReadListAssoc(&val, values, &len);
    std::vector<char *> args;
    for (int i = 0; i < len; i+=2) {
        char * buf = NULL;
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
        auto_sprintf(&buf, "%s=%s", kval, vval);
        free(kval);
        free(vval);
        args.push_back(buf);
    }
    args.insert(args.begin(), (char*)exec_path.c_str());
    args.push_back(nullptr);
    if (ipc_path[0] == '.' && ipc_path[1] == '/') {
        ipc_path = workspace_path + "/" + ipc_path;
    }
    return args;
}

#ifdef __linux__
#include <sys/wait.h>
#include <signal.h>
bool is_proc_kill(int pid, CByondValue &src) {
    int stat = 0;
    if (waitpid(pid, &stat, WNOHANG) == 0) return false;
    if (WIFSIGNALED(stat)) { // everything is fucked
        char * buf = nullptr;
        auto_sprintf(&buf, "spesscore crashed! (%s)", strsignal(WTERMSIG(stat)));
        bwoink(src, buf);
        free(buf);
        return true;
    } else if (WIFEXITED(stat)) { // what? probably an uncaught C# exception, though this should SIGABRT
        char * buf = nullptr;
        auto_sprintf(&buf, "spesscore stopped (exit code: %d)", WEXITSTATUS(stat));
        bwoink(src, buf);
        free(buf);
        return true;
    }
    return false;
}
#elifdef __WIN32__
bool is_proc_kill(int pid, CByondValue &src) {
    // yeah
}
#endif

BYOND_API_METHOD(init) {
    if (argc < 2) return ByondFalse;
    auto src = argv[0];
    auto args = argv[1];
    Core = {}; // clear out our core lol
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
    int fd;
    struct sockaddr_un addr;
    memset(&addr, 0, sizeof(addr));
    addr.sun_family = AF_UNIX;
    strcpy(addr.sun_path, sock_path.c_str());
    // retry for 5 seconds
    clock_t start = clock();
    clock_t end = start+(5*CLOCKS_PER_SEC);
    while (clock() < end) {
        if (is_proc_kill(Core.PID, src)) {
            return ByondFalse;
        }
        if ((fd = socket(PF_UNIX, SOCK_STREAM, 0)) < 0) {
            // bwoink
            bwoink(argv[0], "failed to create socket");
            return ByondFalse;
        }
        if (connect(fd, (struct sockaddr *) &addr, sizeof(addr)) == -1) {
            // bwoink
            close(fd);
        } else {
            Core.Handle = fd;
            return ByondTrue;
        }
    }
    bwoink(argv[0], "failed to open socket");
    return ByondFalse;
}

BYOND_API_METHOD(tick) {
    
}