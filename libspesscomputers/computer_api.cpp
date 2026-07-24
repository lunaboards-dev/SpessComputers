#include "computer_api.hpp"
#include "core.hpp"
#include "ipc.hpp"

BYOND_API_METHOD(create_tty) {
    if (argc < 2) {
        return ByondFalse;
    }
    printf("Create TTY: T: %u, R: %u\n", argv[1].type, argv[1].data.ref);
    TTY term = {
        .ref = argv[1],
        .id = "",
        .status = TTY_STAT_NEED_ID
    };
    Core.TTYCreateRequests.push(term); // we'll get back to you in 7-10 business days
    return ByondTrue;
}

BYOND_API_METHOD(get_tty_id) {
    if (argc < 2) {
        Byond_CRASH("requires two arguments");
        return ByondFalse;
    }
    auto ref = argv[1].data.ref;
    if (!Core.Terminals.contains(ref) || Core.Terminals[ref].status > TTY_STAT_UNOWNED) {
        CByondValue val;
        ByondValue_Clear(&val);
        return val;
    }
    auto id = Core.Terminals[ref].id;
    CByondValue val;
    ByondValue_SetStr(&val, id.c_str());
    return val;
}

BYOND_API_METHOD(create_bios) {
    if (argc < 2) {
        Byond_CRASH("requires two arguments");
        return ByondFalse;
    }
    auto ref = argv[1].data.ref;
    std::string path = "";
    if (argc > 2 && ByondValue_IsStr(argv+2)) {
        u4c size = 0;
        Byond_ToString(argv+2, NULL, &size);
        path.resize(size);
        Byond_ToString(argv+2, path.data(), &size);
    }
    size_t bsize = sizeof(u4c) + path.size();
    void * buffer = sc_alloc(bsize);
    *((u4c *) buffer) = ref;
    memcpy(buffer+sizeof(u4c), path.data(), path.size());
    IPC_Send(argv, IPC_SCreateBIOS, buffer, bsize);
    return ByondTrue;
}