#include "byondapi.h"

#include <string>
#include <vector>
#include <stdarg.h>
#include <string.h>
#include <sys/socket.h>
#include <sys/un.h>

int spess_pid;

#ifdef __linux__
#include <spawn.h>
#include <stdlib.h>
#include <unistd.h>
#include <strings.h>
#include <ctime>
int OpenSpesscore(std::string path, std::vector<char *> args) {
    pid_t pid = 0;
    int ok = posix_spawn(&pid, path.c_str(), NULL, NULL, args.data(), environ);
    spess_pid = pid;
    return ok;
}
#define stricmp strcasecmp
#elifdef __WIN32__
#error "you are so fucked"
#define stricmp _stricmp
#endif

#define NUMBER 0x2A

static CByondValue ByondTrue = {
    .type = NUMBER,
    .data = {.num = 1}
};

static CByondValue ByondFalse = {
    .type = NUMBER,
    .data = {.num = 0}
};

int auto_vsprintf(char ** ptr, const char * fmt, va_list args) {
    int needed = vsnprintf(NULL, 0, fmt, args);
    *ptr = (char*) malloc(needed);
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
    char * buffer = (char*) malloc(len);
    Byond_ToString(&v, buffer, &len);
    return buffer;
}

void super_free(std::vector<char *> args) {
    auto ptr = args.cbegin();
    while (ptr < args.cend()) {
        free(*ptr++);
    }
}

void bwoink(CByondValue &src, const char * msg) {
    CByondValue bwoinks;
    Byond_ReadVar(&src, "bwoinks", &bwoinks);
    // sadness :(
    u4c len = 0;
    Byond_ReadList(&bwoinks, NULL, &len);
    CByondValue pos = {
        .type = NUMBER,
        .data {.num = len}
    };
    CByondValue str;
    ByondValue_SetStr(&str, msg);
    Byond_WriteListIndex(&bwoinks, &pos, &str);
}

extern "C" {
    // args: src, options
    BYOND_EXPORT CByondValue spess_init(int argc, CByondValue* argv) {
        if (argc != 2) return ByondFalse;
        u4c len = 0;
        Byond_ReadListAssoc(argv+1, NULL, &len);
        CByondValue values[len];
        Byond_ReadListAssoc(argv+1, values, &len);
        std::string path = "";
        std::string ws_path = "";
        std::string sock_path = "";
        std::vector<char *> args;
        for (int i = 0; i < len; i+=2) {
            char * buf = NULL;
            char *kval = auto_b2str(values[i]);
            char *vval = auto_b2str(values[i+1]);
            if (stricmp(kval, "execpath") == 0) {
                path = vval;
                free(kval);
                free(vval);
                continue;
            } else if (stricmp(kval, "workspacepath") == 0) {
                ws_path = vval;
            } else if (stricmp(kval, "ipcsocketpath") == 0) {
                sock_path = vval;
                if (sock_path[0] == '.' && sock_path[1] == '/') {
                    sock_path = ws_path + "/" + sock_path;
                }
            }
            auto_sprintf(&buf, "%s=%s", kval, vval);
            free(kval);
            free(vval);
            args.push_back(buf);
        }
        int ok = !OpenSpesscore(path, args);
        super_free(args);
        if (!ok) {
            bwoink(argv[0], "failed to start spesscore");
            return ByondFalse;
        }

        int fd;
        struct sockaddr_un addr;
        memset(&addr, 0, sizeof(addr));
        addr.sun_family = AF_UNIX;
        strcpy(addr.sun_path, sock_path.c_str());
        // retry for 5 seconds
        clock_t start = clock();
        clock_t end = start+(5*CLOCKS_PER_SEC);
        while (clock() < end) {
            if ((fd = socket(PF_UNIX, SOCK_STREAM, 0)) < 0) {
                // bwoink
                bwoink(argv[0], "failed to create socket");
                return ByondFalse;
            }
            if (connect(fd, (struct sockaddr *) &addr, sizeof(addr)) == -1) {
                // bwoink
                close(fd);
            } else {
                return ByondTrue;
            }
        }
        bwoink(argv[0], "failed to open socket");
        return ByondFalse;
    }

    BYOND_EXPORT CByondValue spess_tick(int argc, CByondValue* argv) {
        
    }
}