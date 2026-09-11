#include "utils.hpp"
#include <cstdio>

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