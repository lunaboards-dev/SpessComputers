local f = io.open("test.bin", "w")

local function test_method(x)
    x = x ~ 0xAA55
    x = x * 3
    x = x + 42
    x = x & 0xFFFF
    x = (x << 3) | (x >> 13)
    x = x & 0xFFFF
    return x
end

f:write(string.char(16))
local x = 0
for i=1, 16 do
    f:write((string.pack("H", x)))
    print(x)
    x = test_method(x)
end
local str = os.date()
f:write((string.pack("B", #str)))
f:write(str)
local clk = os.clock()
f:write((string.pack("f>f", clk, os.time())))
print(str, clk)
f:close()