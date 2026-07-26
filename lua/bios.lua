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

local function get_entry(dev, name, parent)
    local reader = dev:query("select * from fmeta where name=? and parent=?", name, parent)
    if reader:length() == 0 then return end
    return reader:next()
end

local function lookup_file(dev, path)
    path = path:gsub("/+", "/"):gsub("/$", ""):gsub("^/", "")
    local parent = {}
    for part in path:gmatch("[^/]+") do
        parent = get_entry(dev, part, parent.inode)
        if not parent then return nil, "not found" end
    end
    return parent
end

local function read_file(dev, name)
    local stat, err = lookup_file(dev, name)
    if not stat then return nil, err end
    local inode, size = stat.inode, stat.size
    local rtv = {}
    while size > 0 do
        local blk = dev:query_first("select data from fdata where inode=? and blkid=?", inode, #rtv)
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