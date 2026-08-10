local _tty = computer.tty()
local tty = peripheral.proxy(_tty)
tty:write("\27[2J\27[H")
tty:write("NT-BIOS v0.13.4444c\r\n")
tty:write("(c) NANOTRASEN 2206\r\n")
tty:write("Only for use on authorized hardware.\r\n")
--tty:write("Strike TAB key to interrupt boot.\r\n")
tty:write(string.format("Memory: %dK\r\n", computer.mem_total()//1024))
local disk = peripheral.proxy(computer.disk())
local q = disk:query("select inode, size from fmeta where name = ? and parent is null", "init.lua")
if q:empty() then
    tty:write("init.lua not found!\r\n")
else
    local ind, size = q:next()
    local blocks = {}
    tty:write(string.format("init.lua(%d): %.1fK\r\n", ind, size/1024))
    local rq = disk:query("select data, blkid from fdata where inode = ? order by blkid asc", ind)
    if rq:empty() then tty:write("ERROR: Corrupt FS!") end
    for blk in rq:values() do
        table.insert(blocks, blk)
    end
    local src = table.concat(blocks):gsub("\0+$", "")
    assert(load(src, "=init.lua"))(disk.id)
end