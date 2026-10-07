local exec = {}

local nopkg = {}

for k, v in pairs(_G) do
    if type(v) ~= "table" then
        nopkg[k] = v
    end
end

function exec.env()
    local env = {
        io = require("core.io"),
        os = require("core.os"),
        math = math,
        table = table,
        string = string,
        package = package,
        debug = debug,
    }
    env._G = env
    for k, v in pairs(nopkg) do
        env[k] = v
    end
    return env
end

function exec.load(path, env)
    env = env or exec.env()
    local func, err = loadfile(path, "t", env)
    if not func then error(err) end
    return coroutine.create(func), env
end

return exec