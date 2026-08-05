local ice = require("ice")
local blake2b = require("blake2b")
local fortuna = {}

local spack, b2hash, mmax, srep, tconcat, tinsert = string.pack, blake2b.hash, math.max, string.rep, table.concat, table.insert

local function counter_inc(self)
    self.counter = self.counter + 1
end

local function counter(self)
    return (spack("l", self.counter))
end

function fortuna:seed(seed)
    self.key = assert(b2hash(seed, mmax(self.level*8, 8), self.key))
    counter_inc(self)
end

function fortuna:generate(amt)
    local buffer = {}
    if amt == 0 then return "" end
    if amt > 0x8000 then
        error("size too large")
    end
    local ikencrypt = self.ik.encrypt
    self.ik.setkey(self.key)
    for i=1, amt, 8 do
        local res = ikencrypt(counter(self))
        counter_inc(self)
        tinsert(buffer, res)
    end

    -- generate new key
    local new_key = ""
    for i=1, self.level do
        new_key = new_key .. ikencrypt(counter(self))
        counter_inc(self)
    end
    self.key = new_key

    return table.concat(buffer)
end

function fortuna.create(level)
    level = level or 8
    return setmetatable({
        level = level,
        key = srep("\0", mmax(level*8, 8)),
        counter = 0,
        ik = ice(level)
    }, {__index=fortuna})
end

return fortuna