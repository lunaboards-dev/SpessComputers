#include "ipc.hpp"
#include "rpc.hpp"
#include <lunarutils/blob.hpp>

struct RPCCallHeader {
    uint32_t dref;
    uint32_t computer;
    uint32_t arg_size;
    uint32_t proc;
    uint16_t argc;
} PACKED;

struct RPCReturnHeader {
    uint32_t computer;
    uint32_t arg_size;
} PACKED;

#define RPC_HEADER_SIZE sizeof(struct RPCCallHeader)
#define RPC_RETURN_SIZE sizeof(struct RPCReturnHeader)


#define IS_NULLABLE(x) (x & RT_NULL)
#define TYPE_MASK ((RPCArgType) 0x7F)
#define REAL_TYPE(x) (x & TYPE_MASK)

bool BoundedRead(void ** ptr, void * out, unsigned int amt, void * max) {
    if (*ptr+amt > max) {
        return false;
    }
    memcpy(out, *ptr, amt);
    *ptr = *ptr + amt;
    return true;
}

bool ReadBasicType(CByondValue &ss, void ** ptr, RPCArgType type, CByondValue *val, void * maxptr) {
    switch (type) {
        case RT_UNDEFINED:
        case RT_NULL:
            ByondValue_Clear(val);
            break;
        case RT_BOOL:
        case RT_INT:
            int vi = 0;
            if (!BoundedRead(ptr, &vi, 4, maxptr)) {
                WTF_BWOINK(ss, "section arg overflow");
                return false;
            }
            ByondValue_SetNum(val, vi);
            break;
        case RT_FLOAT:
            float vf = 0;
            if (!BoundedRead(ptr, &vf, 4, maxptr)) {
                WTF_BWOINK(ss, "section arg overflow");
                return false;
            }
            ByondValue_SetNum(val, vf);
            break;
        case RT_STRING:
            const char * str = (char *) ptr;
            size_t _ptr = (size_t) ptr;
            size_t _maxptr = (size_t) maxptr;
            size_t max_size = _maxptr-_ptr;
            size_t count = strnlen(str, max_size);
            if (count == max_size) {
                WTF_BWOINK(ss, "section arg overflow (no null terminator)");
                return false;
            }
            ByondValue_SetStr(val, str);
            ptr = ptr + count;
            break;
        case RT_BLOB:
            unsigned int vbl = 0;
            if (!BoundedRead(ptr, &vbl, 4, maxptr)) {
                WTF_BWOINK(ss, "section arg overflow");
                return false;
            }
            if (ptr + vbl > maxptr) {
                WTF_BWOINK(ss, "section arg overflow (not enough space for blob)");
                return false;
            }
            void * buffer = malloc(vbl);
            if (!BoundedRead(ptr, &buffer, vbl, maxptr)) {
                WTF_BWOINK(ss, "internal error (blob copy overflow)");
                return false;
            }
            ByondBlob * blob = new MemoryBlob(buffer, vbl);
            blob->set(*val);
            break;
    }
    return true;
}

CByondValue * ReadList(CByondValue &ss, void ** ptr, void * maxptr, int * rlen) {
    int len = 0;
    *rlen = -1;
    if (!BoundedRead(ptr, &len, 4, maxptr)) {
        WTF_BWOINK(ss, "section arg overflow");
        return nullptr;
    }
    if (*ptr + len > maxptr) {
        WTF_BWOINK(ss, "section arg overflow (list: no room for types)");
        return nullptr;
    }
    RPCArgType * types = (RPCArgType*)(*ptr);
    *ptr = *ptr + len;
    CByondValue * arr = (CByondValue*) sc_alloc(sizeof(CByondValue)*len);
    for (int i=0; i < len; i++) {
        if (!ReadBasicType(ss, ptr, types[i], arr+i, maxptr)) {
            return nullptr;
        }
    }
    *rlen = len;
    return arr;
}

RPCArgType WriteBasicValue(CByondValue& ss, std::vector<char> &res, CByondValue * v, RPCArgType expected) {
    int head = res.size();
    if (ByondValue_IsNull(v)) {
        return RT_NULL;
    } else if (ByondValue_IsNum(v)) {
        res.resize(head+4);
        switch ((RPCArgType) (expected & 0x7F)) {
            case RT_INT:
                int wv = v->data.num;
                memcpy(res.data()+head, &wv, 4);
                break;
            default:
                expected = RT_FLOAT;
            case RT_FLOAT:
            case RT_BOOL:
                memcpy(res.data()+head, &v->data, 4);
                break;
        }
        return expected;
    } else if (ByondValue_IsType(v, "/datum/lu_blob")) {
        CByondValue ptr;
        Byond_ReadVar(v, "ptr", &ptr);
        ByondBlob * blob = (ByondBlob*) ptr.data.ref;
        if (!(blob->mode & LU_BLOB_READ)) {
            WTF_BWOINK(ss, "attempt to return write-only blob");
            return RT_NULL;
        }
        int size = blob->size();
        res.resize(head+size+4);
        blob->seek(LU_SEEK_SET, 0);
        memcpy(res.data()+head, &size, 4);
        blob->read(size, res.data()+head+4);
        return RT_BLOB;
    } else {
        unsigned int len = 0;
        Byond_ToString(v, NULL, &len);
        res.resize(head+len);
        Byond_ToString(v, res.data()+head, &len);
        return RT_STRING;
    }
}

RPCArgType WriteList(CByondValue &ss, std::vector<char> &res, CByondValue * v) {
    int head = res.size();
    unsigned int len = 0;
    unsigned int alen = 0;
    Byond_ReadList(v, NULL, &alen);
    Byond_ReadListAssoc(v, NULL, &len);
    if (alen < (len/2)) {
        // use assoc
        CByondValue * arr = (CByondValue *) sc_alloc(sizeof(CByondValue)*len);
        Byond_ReadListAssoc(v, arr, &len);
        res.resize(head+4+len);
        memcpy(res.data()+head, &len, 4);
        for (int i=0; i < len; i++) {
            res[head+4+i] = WriteBasicValue(ss, res, arr+i, RT_UNDEFINED);
        }
        return RT_MAP;
    } else {
        CByondValue * arr = (CByondValue *) sc_alloc(sizeof(CByondValue)*alen);
        Byond_ReadListAssoc(v, arr, &alen);
        res.resize(head+4+alen);
        memcpy(res.data()+head, &alen, 4);
        for (int i=0; i < alen; i++) {
            res[head+4+i] = WriteBasicValue(ss, res, arr+i, RT_UNDEFINED);
        }
        return RT_LIST;
    }
}

bool RPCExecRead(CByondValue& ss, void * blk, size_t sec_size) {
    if (sec_size < RPC_HEADER_SIZE) {
        WTF_BWOINK(ss, "section to small to fit rpc header");
        return false;
    }
    struct RPCCallHeader * hdr = (struct RPCCallHeader *) blk;
    if (sec_size < RPC_HEADER_SIZE+hdr->arg_size+hdr->argc) {
        WTF_BWOINK(ss, "section to small to fit rpc payload");
        return false;
    }
    CByondValue datum = {
        .type = DATUM,
        .data = {
            .ref = hdr->dref
        }
    };
    CByondValue rtv;
    RPCArgType * types = (RPCArgType *)(blk+RPC_HEADER_SIZE);
    CByondValue * args = (CByondValue*) sc_alloc(sizeof(CByondValue)*(hdr->argc+1));
    void * ptr = blk+RPC_HEADER_SIZE+hdr->argc;
    void * maxptr = ptr+hdr->arg_size;
    args[0].type = DATUM;
    args[0].data.ref = hdr->computer;
    for (int arg = 1; arg <= hdr->argc; arg++) {
        RPCArgType type = (RPCArgType) (types[arg-1] & 0x7F);
        if (type < RT_LIST) {
            if (!ReadBasicType(ss, &ptr, type, args+arg, maxptr)) {
                sc_free(args);
                return false;
            }
        } else if (type < RT_INVALID) {
            int alen = 0;
            CByondValue * arr = ReadList(ss, &ptr, maxptr, &alen);
            if (alen == -1) {
                sc_free(args);
                return false;
            }
            Byond_CreateList(args+arg);
            if (type == RT_LIST) {
                Byond_WriteList(args+arg, arr, alen);
            } else if (type == RT_MAP) {
                // GAAAAAAAH FUCKING AAAAAAAH WHY LUMMOX WHY
                if (alen & 1) {
                    WTF_BWOINK(ss, "cannot create an assoc list from an odd number of items");
                    sc_free(arr);
                    sc_free(args);
                    return false;
                }
                for (int i = 0; i < alen; i+=2) {
                    Byond_WriteListIndex(args+arg, arr+i, arr+i+1);
                }
            }
            sc_free(arr);
        } else {
            WTF_BWOINK(ss, std::format("unknown type: {:#02x}", type));
            sc_free(args);
            return false;
        }
    }
    Byond_CallProcByStrId(&datum, hdr->proc, args, hdr->argc, &rtv);
    sc_free(args);
    // handle return value lol
    std::vector<char> pkt;
    struct RPCReturnHeader rhdr = {
        .computer = hdr->computer
    };
    pkt.resize(RPC_RETURN_SIZE+1);
    if (ByondValue_IsList(&rtv)) {
        pkt[RPC_RETURN_SIZE] = WriteList(ss, pkt, &rtv);
    } else {
        pkt[RPC_RETURN_SIZE] = WriteBasicValue(ss, pkt, &rtv, RT_UNDEFINED);
    }
    rhdr.arg_size = pkt.size()-RPC_RETURN_SIZE;
    memcpy(pkt.data(), &rhdr, RPC_RETURN_SIZE);
    IPC_Send(&ss, IPC_SRPC, pkt.data(), pkt.size());
}

IPCSectionHandler RPCHandler = {
    .sec_id = IPC_SRPC,
    .read = RPCExecRead,
    .write = NullWrite
};