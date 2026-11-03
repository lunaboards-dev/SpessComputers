#pragma once
#include <byondapi.h>
#include <string>
#include <stacktrace>

#define PACKED __attribute__((packed))

void bwoink(CByondValue &src, const char * msg);
inline void bwoink(CByondValue &src, std::string msg) {
    bwoink(src, msg.c_str());
}

#define WTF_BWOINK(SSsc, msg) bwoink(SSsc, std::format("WTF!? {}({}:{}) - {}\n", __func__, __FILE__, __LINE__, msg, std::to_string(std::stacktrace::current())))