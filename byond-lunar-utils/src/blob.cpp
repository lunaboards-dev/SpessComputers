#include <lunarutils/blob.hpp>
#include <cstdlib>
#include <cmath>
#include <cstring>
#include <cstdint>
#include <cstdio>
#include <stdexcept>

int ByondBlob::read(int amt, void *buffer) {
    int ramt = this->read_in(this->offset, amt, buffer);
    this->offset += ramt;
    return ramt;
}

int ByondBlob::seek(int whence, int offset) {
    if (whence == 0) { // SEEK_CUR
        this->offset += offset;
    } else if (whence == 1) { // SEEK_SET
        this->offset = offset;
    } else if (whence == 2) { // SEEK_END
        this->offset = this->size() + offset;
    }
    return this->offset;
}

ByondBlob * from_ptr(CByondValue &val) {
    CByondValue pval;
    Byond_ReadVar(&val, "ptr", &pval);
    return (ByondBlob *) pval.data.ref;
}

BYOND_API_METHOD(lu_blob_close) {
    if (argc != 1) {
        Byond_CRASH("close requires src");
        return {};
    }
    CByondValue src = argv[0];
    ByondBlob * blob = from_ptr(src);
    blob->close();
    delete blob;
    return {};
}

BYOND_API_METHOD(lu_blob_read) {
    if (argc != 2) {
        Byond_CRASH("read requires src and amt");
        return {};
    }
    CByondValue src = argv[0];
    CByondValue _amt = argv[1];
    ByondBlob * blob = from_ptr(src);
    int amt = _amt.data.num;
    void * buffer = malloc(amt+1);
    memset(buffer, 0, amt+1);
    int ramt = blob->read(amt, buffer);
    char * str = (char *) buffer;
    CByondValue rval;
    ByondValue_SetStr(&rval, str);
    return rval;
}

BYOND_API_METHOD(lu_blob_read8) {
    if (argc != 2) {
        Byond_CRASH("read requires src and signed");
        return {};
    }
    CByondValue src = argv[0];
    CByondValue _is_sign = argv[1];
    ByondBlob * blob = from_ptr(src);
    bool is_sign = _is_sign.data.num != 0;
    uint8_t buffer = 0;
    int ramt = blob->read(1, &buffer);
    CByondValue rval;
    if (is_sign)
        ByondValue_SetNum(&rval, (int8_t) buffer);
    else
        ByondValue_SetNum(&rval, buffer);
    return rval;
}

BYOND_API_METHOD(lu_blob_read16) {
    if (argc != 3) {
        Byond_CRASH("read requires src, signed, and be");
        return {};
    }
    CByondValue src = argv[0];
    CByondValue _is_sign = argv[1];
    CByondValue _is_be = argv[2];
    ByondBlob * blob = from_ptr(src);
    bool is_sign = _is_sign.data.num != 0;
    bool is_be = _is_be.data.num != 0;
    uint16_t buffer = 0;
    int ramt = blob->read(2, &buffer);
    if (is_be)
        buffer = (buffer >> 8) | (buffer << 8);
    CByondValue rval;
    if (is_sign)
        ByondValue_SetNum(&rval, (int16_t) buffer);
    else
        ByondValue_SetNum(&rval, buffer);
    return rval;
}

BYOND_API_METHOD(lu_blob_readfloat) {
    if (argc != 2) {
        Byond_CRASH("read requires src and be");
        return {};
    }
    CByondValue src = argv[0];
    CByondValue _is_be = argv[1];
    ByondBlob * blob = from_ptr(src);
    bool is_be = _is_be.data.num != 0;
    uint32_t buffer = 0;
    int ramt = blob->read(4, &buffer);
    if (is_be) {
        buffer = ((buffer & 0xFF) << 24) |
                 ((buffer & 0xFF00) << 8) |
                 ((buffer & 0xFF0000) >> 8) |
                 (buffer >> 24);
    }
    CByondValue rval;
    ByondValue_SetNum(&rval, 0);
    rval.data.ref = buffer; // lol
    return rval;
}

BYOND_API_METHOD(lu_blob_seek) {
    if (argc != 3) {
        Byond_CRASH("seek requires src, whence, and offset");
        return {};
    }
    CByondValue src = argv[0];
    CByondValue _whence = argv[1];
    CByondValue _offset = argv[2];
    ByondBlob * blob = from_ptr(src);
    int whence = _whence.data.num;
    int offset = _offset.data.num;
    if (whence < 0 || whence > 2) {
        Byond_CRASH("invalid position to seek from");
        return {};
    }
    int pos = blob->seek(whence, offset);
    CByondValue rval;
    ByondValue_SetNum(&rval, pos);
    return rval;
}

BYOND_API_METHOD(lu_blob_size) {
    if (argc != 1) {
        Byond_CRASH("size requires src");
        return {};
    }
    CByondValue src = argv[0];
    ByondBlob * blob = from_ptr(src);
    int size = blob->size();
    CByondValue rval;
    ByondValue_SetNum(&rval, size);
    return rval;
}

void ByondBlob::set(CByondValue &val) {
    CByondValue path;
    ByondValue_SetStr(&path, "/datum/lu_blob");
    bool ok = Byond_New(&path, nullptr, 0, &val);
    if (!ok) Byond_CRASH("error creating blob object");
    CByondValue ptr;
    ByondValue_SetNum(&ptr, 0);
    ptr.data.ref = (unsigned int) this;
    Byond_WriteVar(&val, "ptr", &ptr);
}

class FileBlob : public ByondBlob {
    private:
        FILE * handle;
        int read_in(int offset, int amt, void * buffer) {
            fseek(handle, SEEK_SET, offset);
            return fread(buffer, 1, amt, handle);
        }
    public:
        int size() override {
            return fseek(handle, SEEK_END, 0);
        }

        void close() override {
            fclose(handle);
        }

        FileBlob(const char * path) {
            handle = fopen(path, "rb");
            if (handle == nullptr) {
                throw std::runtime_error("file not found");
            }
        }
};

BYOND_API_METHOD(lu_blob_openfile) {
    printf("argc: %d\n", argc);
    if (argc != 1) {
        Byond_CRASH("lu_openfile requires a path");
        return {};
    }
    std::string path = ByondValue_ToString(argv);
    ByondBlob * blob = new FileBlob(path.c_str());
    CByondValue val;
    blob->set(val);
    return val;
}