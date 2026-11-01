local socket = require("posix.sys.socket")
local unistd = require("posix.unistd")
local utils = require("utils")

local ipc = {
    fd = -1,
    handlers = {}
}

ipc.msg = {
    create_tty = 0,
    set_id = 1,
    game_state = 2,
    bwoink = 3,
    event = 4,
    ping = 5,
    create_bios = 6,
    rpc = 7,
    register_dev = 8
}

for k, v in pairs(ipc.msg) do
    ipc.handlers[v] = {}
end

function ipc.connect(path)
    if ipc.fd and ipc.fd > 0 then
        unistd.close(ipc.fd)
        ipc.fd = -1
    end
    ipc.fd = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM, 0)
    local ok, err, enum
    local deadline = os.time()+5
    while not ok do
        if os.time() > deadline then
            utils.panic("failed to connect: "..err)
        end
        ok, err, enum = socket.connect(ipc.fd, {
            family = socket.AF_UNIX,
            path = path
        })
    end
end

local sechdr = "HI"

function ipc.send(msg_type, msg)
    local data = sechdr:pack(msg_type, #msg)..msg
    socket.write(ipc.fd, data)
end

function ipc.pump()
    local hdr = socket.recv(ipc.fd, sechdr:packsize(), socket.MSG_PEEK | socket.MSG_DONTWAIT)
    if #hdr == sechdr:packsize() then
        socket.recv(ipc.fd, sechdr:packsize())
    end
    local mtype, len = sechdr:unpack(hdr)
    local data = socket.recv(ipc.fd, len)
    if ipc.handlers[mtype] then
        for i=1, #ipc.handlers[mtype] do
            ipc.handlers[mtype][i](data)
        end
    end
end

function ipc.on(msg_type, handle)
    if ipc.handlers[msg_type] then
        table.insert(ipc.handlers[msg_type], handle)
    end
end

return ipc