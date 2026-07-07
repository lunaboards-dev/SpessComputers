local syscfg = require("syscfg")
local c_create, c_kresume = coroutine.create, coroutine.kresume
local proc = {}
local procs = {}

--[[
proc_ctx = {
    coro:thread,
    cmdline:string,
    uid:int,
    gid:int,
    egid:int,
    euid:int,
    parent:int,
    id:int,
    start:float,
    env:table
}
]]

function proc.context(uid, gid, euid, egid)
    euid = euid or uid
    egid = egid or gid
    
end

function proc.create(code, context)

end

function proc.current()

end

function proc.add(obj)

end

local function reap(pobj, reason)
    if pobj.parent then

    end
end

local deadline = 0
function proc.run()
    while true do
        deadline = computer.time() + syscfg.sched.deadline
        for i=1, #procs do
            local p = procs[i]
            local ok, exit_code, time = pcall(c_kresume, p.coro)
            if not ok then
                reap(p)
            end
        end
    end
end

return proc