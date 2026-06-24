local tty = computer.tty()
tty:write("\27[2J\27[H")
tty:write(string.format("Memory: %dK/%dK\r\n", computer.mem_used()//1024, computer.mem_total()//1024))
tty:write(string.format("No storage found."))
--[[tty:write("NT-BIOS v0.13.4444c\r\n")
tty:write("(c) NANOTRASEN 2206\r\n")
tty:write("Only for use on authorized hardware.\r\n")
tty:write("Strike TAB key to interrupt boot.\r\n")]]
