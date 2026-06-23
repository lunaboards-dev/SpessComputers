#include "byondapi.h"

#include <string>
#include <vector>
#include <stdarg.h>
#include <string.h>

int spess_pid;

#ifdef __linux__
#include <spawn.h>
#include <stdlib.h>
#include <unistd.h>
int OpenSpesscore(std::string path, std::vector<char *> args) {
    pid_t pid = 0;
    int ok = posix_spawn(&pid, path.c_str(), NULL, NULL, args.data(), environ);
    spess_pid = pid;
    return ok;
}
#elifdef __WIN32__
#error "you are so fucked"
#endif

static CByondValue ByondTrue = {
    .type = 0x2A, // NUMBER
    .data = {.num = 1}
};

static CByondValue ByondFalse = {
    .type = 0x2A, // NUMBER
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

extern "C" {
    // args: src, options
    BYOND_EXPORT CByondValue spess_init(int argc, CByondValue* argv) {
        u4c len = 0;
        Byond_ReadListAssoc(argv+1, NULL, &len);
        CByondValue values[len];
        Byond_ReadListAssoc(argv+1, values, &len);
        std::string path = "";
        std::vector<char *> args;
        for (int i = 0; i < len; i+=2) {
            char * buf = NULL;
            char *kval = auto_b2str(values[i]);
            char *vval = auto_b2str(values[i+1]);
            if (strcmp(kval, "execpath")) {
                path = vval;
                free(kval);
                free(vval);
                continue;
            }
            auto_sprintf(&buf, "%s=%s", kval, vval);
            free(kval);
            free(vval);
            args.push_back(buf);
        }
        int ok = !OpenSpesscore(path, args);
        super_free(args);
        return ok ? ByondTrue : ByondFalse;
    }

    BYOND_EXPORT CByondValue spess_tick(int argc, CByondValue* argv) {
        
    }
}