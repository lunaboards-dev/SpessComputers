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
    env:table,
    children:table{__mode=v}
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

local function reap(pobj, reason, no_notify)
    if not no_notify and pobj.parent then
        -- notify parent
    end
    for k, v in pairs(pobj.children) do
        reap(v, reason, true)
    end
    -- close handles
    
end

local deadline = 0
function proc.run()
    while true do
        deadline = computer.time() + syscfg.sched.deadline
        for i=1, #procs do
            local p = procs[i]
            local ok, exit_code, time = pcall(c_kresume, p.coro)
            if not ok then -- task failed, the children must die
                reap(p, "error")
                goto continue
            end
            if not exit_code then goto continue end
            
            ::continue::
        end
    end
end

return proc