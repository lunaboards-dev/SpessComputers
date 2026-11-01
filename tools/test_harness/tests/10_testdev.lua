local rpc = require("rpc")

local dev = rpc.new_dev()

dev:add("test1", rpc.type.bool, {rpc.type.int, rpc.type.int, rpc.type.int}, function(a, b, c)
    return a+b == c
end)

dev:add("log", rpc.type.undefined, {rpc.type.string}, function(s)
    print(s)
end)

rpc.register_dev(dev)