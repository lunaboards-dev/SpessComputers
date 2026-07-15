#include "computer_api.hpp"
#include "core.hpp"

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