local io = {}

local S_CLOSED = 1
local S_READ = 2
local S_WRITE = 4

local hand = {}

local function close_check(self)
    if (self.mode & S_CLOSED > 0) then
        error("attempt to read from closed handle")
    end
end

function hand:read(amt)
    close_check(self)
end

function hand:readln()
    close_check(self)
end

function hand:write(...)
    close_check(self)
end

function hand:writeln(...)
    self:write(...)
    self:write("\n")
end

function hand:seek(whence, offset)
    close_check(self)
end

function hand:flush()
    close_check(self)
end

function hand:close()
    if self.mode & S_CLOSED > 0 then return end
    self:flush()
    self.mode = self.mode | S_CLOSED
end

function io.open(path, mode)
    local dev, path = io.fs.resolve_if_exists(path)
    
end

function io.opendir(path)

end

function io.mkdir(path)

end

function io.popen(cmd, mode)

end

function io.tmpname()

end

return io