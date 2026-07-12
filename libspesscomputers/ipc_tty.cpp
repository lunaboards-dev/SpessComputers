#include "ipc.hpp"
#include "core.hpp"
#include <cstdint>

struct TTYResponse {
    int16_t status;
    u4c ref;
    char id[37];
    uint8_t pad;
} __attribute__((packed));

bool TTYRequestRead(CByondValue& ss, void * blk, size_t sec_size) {
    if (sec_size < sizeof(TTYResponse)) {
        WTF_BWOINK(ss, "response is smaller than sizeof(TTYResponse)");
        return false;
    };
    TTYResponse * res = (TTYResponse *)blk;
    if (!Core.Terminals.contains(res->ref)) {
        WTF_BWOINK(ss, "response is for invalid ref");
        return false;
    };
    TTY term = Core.Terminals[res->ref];
    if (res->status == TTY_STAT_OK) {
        term.id = res->id;
        if (term.status & TTY_STAT_RECOVER) {
            term.status = TTY_STAT_OK;
        } else {
            term.status = TTY_STAT_UNOWNED;
        }
        CByondValue idval;
        ByondValue_SetStr(&idval, res->id);
        Byond_WriteVar(&term.ref, "id", &idval);
    } else {
        term.status = TTY_STAT_KILLED;
    }
    return true;
}

bool TTYRequestWrite(CByondValue& ss, std::stringstream stream) {
    if (Core.TTYCreateRequests.empty()) return false;
    TTY t = Core.TTYCreateRequests.front();
    Core.TTYCreateRequests.pop();
    stream.write((char*)&t.ref, sizeof(u4c));
    Core.Terminals[t.ref.data.ref] = t;
    return true;
}

IPCSectionHandler TTYHandler = {
    .sec_id = IPC_SCreateTTY,
    .read = TTYRequestRead,
    .write = TTYRequestWrite
};