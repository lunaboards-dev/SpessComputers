print("test")
local tty = computer.tty()
tty:write("SpessCore test.\r\n")

local i = 0
while true do
    tty:write("counting up: "..i.."\r")
    i=i+1
    if i > 10000 then break end
end
tty:write("\n")