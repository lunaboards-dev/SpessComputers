local thd = {}

thd.cycle_deadline = 0.1

local threads = {}

function thd.add(name, parent, coro, env, opt)
    table.insert(threads, {
        name = name,
        parent = part,
        coro = coro,
        env = env,
        children = {},
        cmdline = opt.cmdline or "",
        priority = opt.priority or 0,
        signals = {},
        deadline = 0,
        io = {
            open = {},
            stdout = opt.stdout
            stdin = opt.stdin,
            stderr = opt.stderr
        },
        env = {}
    })
end

local function thd_sort(a, b)
    return a.priority > b.priority
end

function thd.run()
    table.sort(threads, thd_sort)
    for i=1, #threads do
        coroutine.kresume(threads)
    end
end

function thd.threads()
    local i = 0
    local t = {}
    for i=1, #threads do
        table.insert(t, threads)
    end
    return function()
        i = i + 1
        return t[i]
    end
end

function thd.current()
    for i=1, #threads do
        if threads[i].coro == coroutine.running() then return threads[i] end
    end
end

return thd