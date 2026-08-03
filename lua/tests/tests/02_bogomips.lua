print("Testing loop speed...")
local i = 0
local deadline = os.clock()+5
while os.clock() < deadline do
    i = i + 1
end
log.debug(string.format("~%.1f iter/s", i/5))