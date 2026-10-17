#include <lunarutils/helpers.hpp>

std::string ByondValue_ToString(CByondValue *val) {
    u4c len = 0;
    Byond_ToString(val, nullptr, &len);
    std::string buffer;
    buffer.resize(len);
    Byond_ToString(val, buffer.data(), &len);
    return buffer;
}