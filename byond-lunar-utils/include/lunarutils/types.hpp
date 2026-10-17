#pragma once

#define NUMBER 0x2A
#define DATUM 0x21

#include <byondapi.h>

static CByondValue ByondTrue = {
    .type = NUMBER,
    .data = {.num = 1}
};

static CByondValue ByondFalse = {
    .type = NUMBER,
    .data = {.num = 0}
};