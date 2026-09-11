#ifdef __linux__
#include "socket.hpp"
#include <sys/socket.h>
#include <sys/un.h>
#include <unistd.h>

class SocketLinux : public IPCSocket {
    private:
        int handle;
    public:
        int write(void * buffer, size_t size) override {
            ssize_t sent = send(handle, buffer, size, 0);
            if (sent < 0) {
                if (errno == ECONNRESET) {
                    return ISOCK_CLOSED;
                }
                return ISOCK_IOERR;
            }
            if (sent != size) return ISOCK_IOERR;
            return ISOCK_OK;
        }

        int read_sync(void * buffer, size_t size) override {
            ssize_t read = recv(handle, buffer, size, 0);
            if (read < 0) {
                if (errno == ECONNRESET) {
                    return ISOCK_CLOSED;
                }
                return ISOCK_IOERR;
            }
            if (read != size) return ISOCK_IOERR;
            return ISOCK_OK;
        }

        int read_async(void * buffer, size_t * size, bool peek) override {
            ssize_t read = recv(handle, buffer, *size, MSG_DONTWAIT | (peek ? MSG_PEEK : 0));
            if (read < 0) {
                if (errno == ECONNRESET) {
                    return ISOCK_CLOSED;
                }
                return ISOCK_IOERR;
            }
            *size = read;
            return ISOCK_OK;
        }

        int try_connect(std::string path) override {
            struct sockaddr_un addr;
            memset(&addr, 0, sizeof(addr));
            addr.sun_family = AF_UNIX;
            if (path.length() > sizeof(addr.sun_path)-1) {
                return ISOCK_PATH_TOO_LONG;
            }
            strcpy(addr.sun_path, path.c_str());
            if ((handle = socket(PF_UNIX, SOCK_STREAM, 0)) < 0) {
                // womp womp
                return ISOCK_BILLIONS_MUST_DIE;
            }
            if (connect(handle, (struct sockaddr *) &addr, sizeof(addr)) == -1) {
                close(handle);
                if (errno == ECONNREFUSED) {
                    return ISOCK_REFUSED;
                }
            }
            return ISOCK_OK;
        }
};

SocketLinux sock;
IPCSocket * MainSocket = &sock;
#endif