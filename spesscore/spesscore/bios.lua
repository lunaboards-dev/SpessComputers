local tty = computer.tty()
tty:write("\27[2J\27[H")
tty:write(string.format("Memory: %dK/%dK\r\n", computer.mem_used()//1024, computer.mem_total()//1024))
tty:write(string.format("No storage found.\r\n"))
--[[tty:write("NT-BIOS v0.13.4444c\r\n")
tty:write("(c) NANOTRASEN 2206\r\n")
tty:write("Only for use on authorized hardware.\r\n")
tty:write("Strike TAB key to interrupt boot.\r\n")]]

local nm = os.clock()
local i = 0

tty:write("Counting to at least 1000.\r\n")

local c = coroutine.create(function()
    while true do
        tty:write(string.format("%i\r", i))
        i = i + 1
    end
end)

while i < 1000 do
    coroutine.kresume(c)
end

tty:write("^ Should be over 1000.\r\n")

--[[ tty:write("> ")

while true do
    local inpt = tty:next()
    if inpt then
        print("yerp: "..#inpt)
        tty:write(inpt)
    end
end ]]