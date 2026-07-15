#include "ipc.hpp"
#include "core.hpp"

struct SetIDRes {
    u4c ref;
    char id[37];
    u1c pad;
} __attribute__((packed));

bool SetIDRead(CByondValue& ss, void * blk, size_t sec_size) {
    if (sec_size < sizeof(SetIDRes)) {
        WTF_BWOINK(ss, "got SetID packet smaller than struct SetIDRes");
        return false;
    }
    SetIDRes * res = (SetIDRes*) blk;
    CByondValue val = {
        .type = DATUM,
        .data = {
            .ref = res->ref
        }
    };
    if (!Byond_TestRef(&val)) {
        WTF_BWOINK(ss, "got SetID packet for invalid ref");
        return false;
    }
    CByondValue idv;
    ByondValue_SetStr(&idv, res->id);
    if (!ByondValue_IsStr(&idv)) {
        WTF_BWOINK(ss, "string to be set is not string");
    }
    if (!Byond_WriteVar(&val, "id", &idv)) {
        WTF_BWOINK(ss, "failed to set var");
    }
    // temp
    u4c count;
    Byond_ToString(&val, NULL, &count);
    char * buf = (char*) sc_alloc(count);
    Byond_ToString(&val, buf, &count);
    printf("(DEBUG) SetID of ref %u (%s) to %s\n", res->ref, buf, res->id);
    sc_free(buf);
    return true;
}

IPCSectionHandler SetIDHandler = {
    .sec_id = IPC_SSetID,
    .read = SetIDRead,
    .write = NullWrite
};