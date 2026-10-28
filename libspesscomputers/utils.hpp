#pragma once
#include <byondapi.h>
#include <string>

#define PACKED __attribute__((packed))

void bwoink(CByondValue &src, const char * msg);
inline void bwoink(CByondValue &src, std::string msg) {
    bwoink(src, msg.c_str());
}

#define WTF_BWOINK(SSsc, msg) bwoink(SSsc, std::format("WTF!? {}({}:{}) - {}", __func__, __FILE__, __LINE__, msg))