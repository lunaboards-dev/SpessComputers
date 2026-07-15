#include "ipc.hpp"

#ifdef __WIN32__
#define MSG_DONTWAIT 0 // i should fucking kill someone
#warning "windows fucking sucks"
#endif

struct ipc_header {
    uint16_t sectype;
    uint32_t len;
} __attribute__((packed));

bool IPC_Send(CByondValue * ss, IPCSectionID id, void * buffer, size_t size) {
    ipc_header hdr = {
        .sectype = id,
        .len = size
    };
    if (send(Core.Handle, &hdr, sizeof(ipc_header), 0) < sizeof(ipc_header)) {
        WTF_BWOINK(*ss, "somehow sent less bytes than sizeof(ipc_header)");
        return false;
    }
    if (send(Core.Handle, buffer, size, 0) < size) {
        WTF_BWOINK(*ss, "somehow sent less bytes than size of buffer");
        return false;
    }
    return true;
}

void IPC_Flush(CByondValue * ss) {
    auto hand = Handlers;
    while (*hand != nullptr) {
        int ctr = 0;
        while (true) {
            std::stringstream str;
            if (!(*hand)->write(*ss, str, &ctr)) break;
            auto buffer = str.str();
            IPC_Send(ss, (*hand)->sec_id, (void*)buffer.c_str(), buffer.size());
        }
        hand++;
    }
}

ipc_header rcv_buffer;

bool IPC_Next(CByondValue * ss) {
    auto count = recv(Core.Handle, &rcv_buffer, sizeof(ipc_header), MSG_DONTWAIT | MSG_PEEK); // this is retarded
    // C++ i swear to god
    if (count < ((ssize_t)sizeof(ipc_header))) {
        return false;
    }
    // actually consume the buffer
    recv(Core.Handle, &rcv_buffer, sizeof(ipc_header), 0);
    printf("(C++ DEBUG) IPC: Type %u, Size %u\n", rcv_buffer.sectype, rcv_buffer.len);
    // we can block here now
    void * ptr = sc_alloc(rcv_buffer.len);
    recv(Core.Handle, ptr, rcv_buffer.len, 0);
    auto hand = Handlers;
    while (*hand != nullptr) {
        if ((*hand)->sec_id == rcv_buffer.sectype) { // somehow C++ is smarter about casing enums to their inherited types than C#
            if (!(*hand)->read(*ss, ptr, rcv_buffer.len)) {
                WTF_BWOINK(*ss, "got corrupted packet!");
            }
            return true;
        }
        hand++;
    }
    WTF_BWOINK(*ss, "got unknown packet");
    return false;
}

BYOND_API_METHOD(ipc_pump) {
    if (argc < 1) return ByondFalse;
    try {
        return IPC_Next(argv) ? ByondTrue : ByondFalse;
    } catch (const std::runtime_error& e) {
        bwoink(argv[0], std::format("c++ exception: {}", e.what()).c_str());
        return ByondFalse;
    } catch (...) {
        return ByondFalse;
    }
}

bool NullWrite(CByondValue& ss, std::stringstream& stream,int*ctr) {
    return false;
}

bool NullRead(CByondValue&,void*,size_t) {
    return false;
}

IPCSectionHandler* Handlers[] = {
    &TTYHandler,
    &SetIDHandler,
    /* &PingHandler,
    &PongHandler,
    &EventHandler, */
    nullptr
};