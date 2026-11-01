local rpc = {}

local ref = require("ref")
local ipc = require("ipc")

rpc.type = {
    undefined = 0,
    int = 1,
    float = 2,
    bool = 3,
    string = 4,
    blob = 5,
    list = 6,
    map = 7,
    invalid = 8,
    null = 128
}

function rpc.register_dev(proto)
    local id = ref.add(proto)
end

return rpc