---@alias kthd_io {open: table[], stdout: table, stdin: table, stderr: table}
---@alias kthd {name: string, parent: kthd, coro: thread, env: table, children: kthd[],
--- cmdline: string, priority: number, signals: table, deadline: number, io: kthd_io,
--- evars: table<string, any>}
local thd = {}

thd.cycle_deadline = 0.1
--- Threads
--- @type kthd[]
local threads = {}

---@return kthd
function thd.add(name, parent, coro, env, opt)
    local thd = {
        name = name,
        parent = parent,
        coro = coro,
        env = env,
        children = {},
        cmdline = opt.cmdline or "",
        priority = opt.priority or 0,
        signals = {},
        deadline = 0,
        io = {
            open = {},
            stdout = opt.stdout,
            stdin = opt.stdin,
            stderr = opt.stderr
        },
        evars = {}
    }
    table.insert(threads, thd)
    return thd
end

local function thd_sort(a, b)
    return a.priority > b.priority
end

function thd.run()
    table.sort(threads, thd_sort)
    for i=1, #threads do
        coroutine.kresume(threads[i].coro)
    end
end

function thd.threads()
    local i = 0
    local t = {}
    for i=1, #threads do
        table.insert(t, threads[i])
    end
    ---@return kthd|nil
    return function()
        i = i + 1
        return t[i]
    end
end

function thd.current()
    for i=1, #threads do
        if threads[i].coro == coroutine.running() then return threads[i] end
    end
    return nil
end

function thd.sleep(amt)
    
end

return thd