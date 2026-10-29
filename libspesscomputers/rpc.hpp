#pragma once

enum RPCArgType : uint8_t {
    RT_UNDEFINED = 0,
    RT_INT,
    RT_FLOAT,
    RT_BOOL,
    RT_STRING,
    RT_BLOB,
    RT_LIST,
    RT_MAP,
    RT_INVALID,
    RT_NULL = 128
};

struct SCMethodInfo {
    std::string path;
    unsigned int refid;
    RPCArgType rettype;
    std::vector<RPCArgType> args;
};

struct SCDevInfo {
    std::string name;
    unsigned int refid;
    std::vector<SCMethodInfo> methods;
};