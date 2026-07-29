local tty = computer.tty()
tty:write("\27[2J\27[H")
tty:write(string.format("Memory: %dK/%dK\r\n", computer.mem_used()//1024, computer.mem_total()//1024))
tty:write(string.format("Atempting to load local storage...\r\n"))
--[[tty:write("NT-BIOS v0.13.4444c\r\n")
tty:write("(c) NANOTRASEN 2206\r\n")
tty:write("Only for use on authorized hardware.\r\n")
tty:write("Strike TAB key to interrupt boot.\r\n")]]

--[[local rtv = computer.disk():query("select * from fmeta where name=?", "init.lua"):read()
for k, v in pairs(rtv) do
    tty:write(string.format("%s\t%q\r\n", k, v))
end]]

--[[ tty:write("> ")

while true do
    local inpt = tty:next()
    if inpt then
        print("yerp: "..#inpt)
        tty:write(inpt)
    end
end ]]