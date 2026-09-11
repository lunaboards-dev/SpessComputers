#pragma once

#include <string>

#define ISOCK_OK 0
#define ISOCK_CLOSED 1
#define ISOCK_IOERR 2
#define ISOCK_REFUSED 3
#define ISOCK_PATH_TOO_LONG 4
#define ISOCK_BILLIONS_MUST_DIE 5
#define ISOCK_WOULD_BLOCK -1

class IPCSocket {
    public:
        // Write SHOULD be synchronous but it being async shouldn't be an issue
        virtual int write(void * buffer, size_t size) = 0;
        // Blocks until all the data is read into the buffer or a socket exception happens.
        // This exists because I'm lazy.
        virtual int read_sync(void * buffer, size_t size) = 0;
        // Does NOT block, but will return ISOCK_WOULD_BLOCK if it the buffer isn't full.
        // If peek is true, shouldn't consume the underlying socket buffer.
        virtual int read_async(void * buffer, size_t * size, bool peek) = 0;
        // Connects to a socket at the specified path. Duh.
        // This will be retried multiple times if it returns ISOCK_REFUSED
        virtual int try_connect(std::string path);
};

extern IPCSocket * MainSocket;