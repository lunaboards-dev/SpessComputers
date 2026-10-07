local os = {}

local sched = require("core.sched")

function os.getenv(var)
    local node = sched.current()
    while node do
        local v = node.evars[var]
        if v then return v end
        node = node.parent
    end
end

function os.setenv(var, val)
    local node = sched.current()
    if not node then error("can't setenv outside of thread") end
    node.evars[var] = val
end

function os.execute(cmd)

end

return os