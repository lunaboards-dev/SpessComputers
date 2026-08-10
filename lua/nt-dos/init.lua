local dev = ...

local rdev = peripheral.proxy(dev)

local tty = computer.tty()

local function dprint(msg)
    if tty then
        peripheral.call(tty, "write", msg.."\r\n")
    end
end

local function panic(msg)
    dprint("PANIC!: "..msg)
    while true do end
end

local function rlookup(parent, entry)
    local pid
    if parent and parent > 0 then pid = parent end
    local res = rdev:query("select * from fmeta where name=? and "..(pid and "parent=?" or "parent is null"), table.unpack {entry, pid})
    if res:empty() then return nil end
    return res:read()
end

local function lookup_file(path)
    local parent = {}
    for m in path:gmatch("[^/]+") do
        local st = rlookup(parent.inode, m)
        if not st then return panic("NOT FOUND: "..path) end
        parent = st
    end
    return parent
end

local function read_file(path)
    local res = {}
    local st = lookup_file(path)
    local q = rdev:query("select data, blkid from fdata where inode=? order by blkid asc", st.inode)
    for blk in q:values() do
        table.insert(res, blk)
    end
    return table.concat(res):gsub("\0+$", "")
end

local function load_file(path)
    return assert(load(read_file(path), "="..path))
end

dprint("NTDOS 9.17")

local _fs = load_file("/lib/core/manfs.lua")()
local fsobj = setmetatable({dev=rdev}, {__index=_fs})
local bootobj = {
    root = fsobj,
    loadfile = load_file,
    readfile = read_file,
    status = dprint
}

local scr = {}
for ent in fsobj:opendir("/boot") do
    table.insert(scr, "/boot/"..ent)
end
table.sort(scr)

for i=1, #scr do
    dprint("> "..scr[i])
    load_file(scr[i])(bootobj)
end

error("returned to init!")