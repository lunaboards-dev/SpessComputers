#include <lunarutils/helpers.hpp>
#include "ipc.hpp" // order matters due to me being retarded with macros
#include "rpc.hpp"

struct DevInfoHeader {
    uint32_t refid;
    uint16_t mcount;
    uint16_t pathlen;
} PACKED;

#define DI_HEADER_SIZE sizeof(struct DevInfoHeader)

struct MethodInfoHeader {
    uint32_t refid;
    RPCArgType rtv;
    uint8_t acount;
    uint16_t pathlen;
} PACKED;

#define MI_HEADER_SIZE sizeof(struct MethodInfoHeader)

void DevReadInfo(CByondValue * ss, CByondValue * dev, struct SCDevInfo &devinfo) {
    CByondValue name;
    Byond_ReadVar(dev, "name", &name);
    devinfo.refid = name.data.ref;
    devinfo.name = ByondValue_ToString(&name);
    CByondValue methods;
    Byond_ReadVar(dev, "callbacks", &methods);
    unsigned int methodc = 0;
    Byond_ReadListAssoc(&methods, NULL, &methodc);
    CByondValue * values = (CByondValue *) sc_alloc(sizeof(CByondValue)*methodc);
    for (int i = 0; i < methodc; i+=2) {
        CByondValue rtv, args;
        Byond_ReadVar(values+i+1, "rtype", &rtv);
        Byond_ReadVar(values+i+1, "atype", &args);
        unsigned int acount = 0;
        Byond_ReadList(&args, NULL, &acount);
        CByondValue * argl = (CByondValue*) sc_alloc(sizeof(CByondValue)*acount);
        Byond_ReadList(&args, argl, &acount);

        struct SCMethodInfo minfo;
        minfo.refid = values[i].data.ref;
        minfo.path = ByondValue_ToString(values+i);
        int rt = rtv.data.num;
        minfo.rettype = (RPCArgType) rt;

        for (int j=0; j<acount; j++) {
            int at = argl[j].data.num;
            minfo.args.push_back((RPCArgType) at);
        }
        sc_free(argl);
        devinfo.methods.push_back(minfo);
    }
    sc_free(values);
    Core.DeviceInfo.push_back(devinfo);
}

BYOND_API_METHOD(dev_register) {
    if (argc != 2) {
        Byond_CRASH("dev_register needs src and dev");
    }

    CByondValue * ss = argv;
    CByondValue * dev = argv+1;

    struct SCDevInfo info;
    DevReadInfo(ss, dev, info);

    std::vector<char> buffer;

    struct DevInfoHeader hdr;
    hdr.refid = info.refid;
    hdr.mcount = info.methods.size();
    hdr.pathlen = info.name.length();

    buffer.resize(MI_HEADER_SIZE+info.name.length());
    memcpy(buffer.data(), &hdr, MI_HEADER_SIZE);
    memcpy(buffer.data()+MI_HEADER_SIZE, info.name.c_str(), hdr.pathlen);

    for (int i=0; i < info.methods.size(); i++) {
        struct MethodInfoHeader mhdr;
        int head = buffer.size();
        struct SCMethodInfo &mi = info.methods[i];
        buffer.resize(head+MI_HEADER_SIZE+mi.args.size()+mi.path.length());
        mhdr.refid = mi.refid;
        mhdr.rtv = mi.rettype;
        mhdr.acount = mi.args.size();
        mhdr.pathlen = mi.path.length();
        memcpy(buffer.data()+head, &mhdr, MI_HEADER_SIZE);
        memcpy(buffer.data()+head+MI_HEADER_SIZE, mi.args.data(), mhdr.acount);
        memcpy(buffer.data()+head+MI_HEADER_SIZE+mhdr.acount, mi.path.c_str(), mhdr.pathlen);
    }

    IPC_Send(ss, IPC_SRegisterDev, buffer.data(), buffer.size());
}