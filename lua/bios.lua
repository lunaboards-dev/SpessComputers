local lty = computer.tty()
local vtwrite = function()end
if lty then
    vtwrite = function(str)
        lty:write(str)
    end
end

local function print(str)
    vtwrite(str:gsub("\n", "\r\n").."\r\n")
end

local function eprint(str)
    print(string.format("\27[31m%s\27[0m", str))
end

print[[NT-BIOS v0.13.6444c
(c) NANOTRASEN 2206
For use on authorized hardware only.
Strike TAB to interrupt boot
]]

local bank = 128
vtwrite(string.format("MEMORY TEST: %d ...", bank*512))
while computer.test_bank(bank) do
    bank = bank + 1
    vtwrite(string.format("\rMEMORY TEST: %d ...", bank*512))
end

comptuer.set_mem_size(bank)

print(" \27[1;32mPASSED!\27[0m")

local function finfo(dev, store, name)
    local inode, size = dev:query(string.format("SELECT inode, size FROM meta WHERE store=%q AND name=%q", store, name))
    if not inode then return nil, "not found" end
    return inode, size
end

local function read_file(dev, store, name)
    local inode, size = finfo(dev, store, name)
    if not inode then return nil, size end
    local rtv = {}
    while size > 0 do
        local blk = dev:query(string.format("SELECT data FROM fdat WHERE inode=%q and block=%q", inode, #rtv))
        if not blk then return nil, "corrupt file record" end
        table.insert(rtv, blk)
        size = size - #blk
    end
    return table.concat(rtv)
end

local function load_init(dev)
    if finfo(fd, "", "init.lua") then return end
    local data, err = read_file(dev, "", "init.lua")
    if not data then
        eprint("I/O error: "..err)
        return
    end
    local res, err = load(data, "=init.lua")
    if not res then
        eprint("Error loading boot code: "..err)
        return
    end
    res(dev)
end

local lfd = computer.fdd()

if lfd then
    local fd = lfd.disk()
    if fd then 
        load_init(fd)
    end
end

local ldd = computer.disk()

if ldd then
    load_init(ldd)
end

eprint("No bootable media.")
while true do computer.pullSignal() end