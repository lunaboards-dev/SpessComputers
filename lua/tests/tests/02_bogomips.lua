print("Testing loop speed...")
local i = 0
local deadline = os.time()+5
while os.time() < deadline do
    i = i + 1
end
log.debug(string.format("~%.1f iter/s", i/5))