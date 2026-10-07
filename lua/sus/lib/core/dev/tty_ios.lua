local tty = {}

local sched = require("core.sched")

function tty:write(...)

end

function tty:read(amt)

end

function tty:destroy()
    self.destroyed = true
end

local function tty_create(dev)
    sched.add("tty_ios", sched.current(), coroutine.create(function()
        -- yeah
    end), _G, {})
end

return tty_create