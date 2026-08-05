local function checkArg(n, have, ...)
	have = type(have)
	local function check(want, ...)
		if not want then
			return false
		else
			return have == want or check(...)
		end
	end
	if not check(...) then
		local msg = string.format("bad argument #%d (%s expected, got %s)",
															n, table.concat({...}, " or "), have)
		error(msg, 3)
	end
end

local critical = system.critical

local function tty_write(str)
	peripheral.call(computer.tty(), "write", str)
end

local function tty_writeln(str)
	tty_write(str.."\r\n")
end

local _xpcall = xpcall
function ypcall(f, errh, ...)
	checkArg(1, f, "function")
	checkArg(2, errh, "function")
	local xerr, xdtb
	local c_errh = critical(function(err)
		xerr = err
		xdtb = debug.traceback(err)
	end)
	local res = table.pack(_xpcall(f, c_errh, ...))
	if not res[1] then
		errh(xerr, xdtb)
	end
	return table.unpack(res)
end

local rare_fox = system.rare_fox

local function gatekeeper(func) -- this is required for debug functions
    local res = func() -- break tailcall
    return res
end

local function bit_cast(from, to, ...)

end