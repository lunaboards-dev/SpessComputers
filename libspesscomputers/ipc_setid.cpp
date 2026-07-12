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
    Byond_WriteVar(&val, "id", &idv);
    return true;
}

IPCSectionHandler SetIDHandler = {
    .sec_id = IPC_SSetID,
    .read = SetIDRead,
    .write = NullWrite
};