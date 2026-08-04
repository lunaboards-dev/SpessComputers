local shell = {}

function shell.new(tty)
    return setmetatable({
        buffer = "",
        curpos = 1,
        history = {},
        tty = peripheral.proxy((type(tty) == "table") and tty.id or tty),
    }, {__index=shell})
end

function shell:get_curpos()
    self.tty:write("\27[6n")
    ::curpos::
    local esc
    while not esc do
        esc = self.tty:next()
    end
    local y, x = esc:match("^\27%[(%d+);(%d+)R$")
    if not x then goto curpos end
    return x, y
end

function shell:read()
    local tin = self.tty:next()
    if not tin then return end
    local bin = tin:byte()
    if bin == 27 then
        -- Parse escape code
        self.tty:write(tin:sub(2)) -- or don't
    elseif bin > 31 and bin < 127 then
        self.buffer = self.buffer .. tin
        self.tty:write(tin)
    elseif bin == 127 then
        local ol = #self.buffer
        self.buffer = self.buffer:sub(1, -2)
        local nl = #self.buffer
        if ol ~= nl then
            local x, y = self:get_curpos()
            if x == "1" then
                self.tty:write("\27[2K\27[F\27[200C\27[D\27[K")
            else
                self.tty:write("\27[D\27[K")
            end
        end
    elseif bin == 13 then
        local r = self.buffer
        self.buffer = ""
        return r
    else
        self.tty:write(string.format("~x%.2x", bin))
    end
end

function shell:readline()
    self.tty:write("> ")
    while true do
        local line = self:read()
        if line then return line end
    end
end

return shell