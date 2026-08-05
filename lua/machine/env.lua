local sandbox = {
	ipairs = ipairs,
	next = next,
	pairs = pairs,
	rawequal = rawequal,
	rawget = rawget,
	rawlen = rawlen,
	rawset = rawset,
	select = select,
	tonumber = tonumber,
	tostring = tostring,
	type = type,
	assert = assert,
	getmetatable = getmetatable,
	coroutine = cr,
	string = {
		byte = string.byte,
		char = string.char,
		dump = string.dump,
		find = slib.find,
		format = string.format,
		gmatch = slib.gmatch,
		gsub = slib.gsub,
		len = string.len,
		lower = string.lower,
		match = slib.match,
		rep = string.rep,
		reverse = string.reverse,
		sub = string.sub,
		upper = string.upper,
		pack = string.pack,
		unpack = string.unpack,
		packsize = string.packsize
	},
	table = {
		concat = table.concat,
		insert = table.insert,
		pack = table.pack,
		remove = table.remove,
		sort = table.sort,
		unpack = table.unpack,
		move = table.move
	},
	math = {
		abs = math.abs,
		acos = math.acos,
		asin = math.asin,
		atan = math.atan,
		atan2 = math.atan2 or math.atan, -- Deprecated in Lua 5.3
		ceil = math.ceil,
		cos = math.cos, -- Deprecated in Lua 5.3
		cosh = math.cosh or function(x)
			checkArg(1, x, "number")
			return (math.exp(x) + math.exp(-x)) / 2
		end,
		deg = math.deg,
		exp = math.exp,
		floor = math.floor,
		fmod = math.fmod,
		frexp = math.frexp or l54_frexp, -- Deprecated in Lua 5.3
		huge = math.huge,
		ldexp = math.ldexp or function(a, e) -- Deprecated in Lua 5.3
			checkArg(1, x, "number")
			return a*(2.0^e)
		end,
		log = math.log,
		max = math.max,
		min = math.min,
		modf = math.modf,
		pi = math.pi,
		pow = math.pow or function(a, b) -- Deprecated in Lua 5.3
			checkArg(1, x, "number")
			return a^b
		end,
		rad = math.rad,
		random = math.random,
		randomseed = math.randomseed,
		sin = math.sin,
		sinh = math.sinh or function(x) -- Deprecated in Lua 5.3
			checkArg(1, x, "number")
			return (math.exp(x) - math.exp(-x)) / 2
		end,
		sqrt = math.sqrt,
		tan = math.tan,
		tanh = math.tanh or function(x) -- Deprecated in Lua 5.3
			checkArg(1, x, "number")
			local e2x = math.exp(2 * x)
			return (e2x - 1) / (e2x + 1)
		end,
		-- Lua 5.3.
		maxinteger = math.maxinteger,
		mininteger = math.mininteger,
		tointeger = math.tointeger,
		type = math.type,
		ult = math.ult
	},
	os = {
		date = os.date,
		difftime = function(t2, t1)
			return t2 - t1
		end,
		time = function(table)
			checkArg(1, table, "table", "nil")
			return os.time(table)
		end
	},
	utf8 = {
		char = utf8.char,
		charpattern = utf8.charpattern,
		codes = utf8.codes,
		codepoint = utf8.codepoint,
		len = utf8.len,
		offset = utf8.offset
	},
	check_arg = checkArg,
	checkArg = checkArg,
	ypcall = ypcall,
	computer = { -- make sure we don't pass any internal functions to the sandbox
		eeprom = computer.eeprom,
		tty = computer.tty,
		disk = computer.disk,
		mem_total = computer.mem_total,
		mem_used = computer.mem_used,
		mem_free = function() return computer.mem_total()-computer.mem_used() end,
		pull_signal = computer.pull_signal
	},
	peripheral = peripheral -- this one is safe
}

function sandbox.load(chunk, chunkname, mode, env)
	return load(chunk, chunkname, "t", sandbox or env)
end

function sandbox.setmetatable(obj, meta)
	checkArg(1, obj, "table") -- only allow this to be used on tables
	checkArg(2, meta, "table", "nil")
	if meta then
		rawset(meta, "__gc", nil) -- no gc hooks
	end
	return setmetatable(obj, meta)
end