local proxy_cache = setmetatable({}, {__mode="v"})

function peripheral.proxy(id)
    checkArg(1, id, "string")
    if proxy_cache[id] then return proxy_cache[id] end
	local meths = peripheral.methods(id)
	local t = {id=id}
    proxy_cache[id] = t
	for i=1, #meths do
		t[meths[i]] = function(self, ...)
			return peripheral.call(id, meths[i], ...)
		end
	end
	return t
end