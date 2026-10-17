#pragma once

#include <byondapi.h>
#include <string>

#define BYOND_API_METHOD(method) CByondValue method(u4c argc, CByondValue * argv)
#define BYOND_API_DEF(method) extern "C" BYOND_EXPORT BYOND_API_METHOD(method);

std::string ByondValue_ToString(CByondValue *val);