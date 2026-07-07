print("machine.lua")
--[[ computer = {}

local c = _computer
for k, v in pairs(getmetatable(_computer).__index) do
	local func = v
	computer[k] = function(...)
		return v(c, ...)
	end
end
_computer = nil ]]
-- we do all the init here

-- taken straight from OC's machine.lua
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

--[[ This is pretty much a straight port of Lua's pattern matching code from
		 the standard PUC-Rio C implementation. We want to have this in plain Lua
		 for the sandbox, so that timeouts also apply while matching stuff, which
		 can take a looong time for certain "evil" patterns.
		 It passes the pattern matching unit tests from Lua 5.2's test suite, so
		 that should be good enough. ]]
do
	local CAP_UNFINISHED = -1
	local CAP_POSITION = -2
	local L_ESC = '%'
	local SPECIALS = "^$*+?.([%-"
	local SHORT_STRING = 500 -- use native implementations for short strings

	local string_find, string_lower, string_match, string_gmatch, string_gsub =
				string.find, string.lower, string.match, string.gmatch, string.gsub

	local match -- forward declaration

	local strptr
	local strptr_mt = {__index={
		step = function(self, count)
			self.pos = self.pos + (count or 1)
			return self
		end,
		head = function(self, len)
			return string.sub(self.data, self.pos, self.pos + (len or self:len()) - 1)
		end,
		len = function(self)
			return #self.data - (self.pos - 1)
		end,
		char = function(self, offset)
			local pos = self.pos + (offset or 0)
			if pos == #self.data + 1 then
				return "\0"
			end
			return string.sub(self.data, pos, pos)
		end,
		copy = function(self, offset)
			return strptr(self.data, self.pos + (offset or 0))
		end
		},
		__add = function(a, b)
			if type(b) == "table" then
				return a.pos + b.pos
			else
				return a:copy(b)
			end
		end,
		__sub = function(a, b)
			if type(b) == "table" then
				return a.pos - b.pos
			else
				return a:copy(-b)
			end
		end,
		__eq = function(a, b)
			return a.data == b.data and a.pos == b.pos
		end,
		__lt = function(a, b)
			assert(a.data == b.data)
			return a.pos < b.pos
		end,
		__le = function(a, b)
			assert(a.data == b.data)
			return a.pos <= b.pos
		end
	}
	function strptr(s, pos)
		return setmetatable({
			data = s,
			pos = pos or 1
		}, strptr_mt)
	end

	local function islower(b) return b >= 'a' and b <= 'z' end
	local function isupper(b) return b >= 'A' and b <= 'Z' end
	local function isalpha(b) return islower(b) or isupper(b) end
	local function iscntrl(b) return b <= '\007' or (b >= '\010' and b <= '\017') or (b >= '\020' and b <= '\027') or (b >= '\030' and b <= '\037' and b ~= ' ') or b == '\177' end
	local function isdigit(b) return b >= '0' and b <= '9' end
	local function ispunct(b) return (b >= '{' and b <= '~') or (b == '`') or (b >= '[' and b <= '_') or (b == '@') or (b >= ':' and b <= '?') or (b >= '(' and b <= '/') or (b >= '!' and b <= '\'') end
	local function isspace(b) return b == '\t' or b == '\n' or b == '\v' or b == '\f' or b == '\r' or b == ' ' end
	local function isalnum(b) return isalpha(b) or isdigit(b) end
	local function isxdigit(b) return isdigit(b) or (b >= 'a' and b <= 'f') or (b >= 'A' and b <= 'F') end
	local function isgraph(b) return not iscntrl(b) and not isspace(b) end

	-- translate a relative string position: negative means back from end
	local function posrelat(pos, len)
		if pos >= 0 then return pos
		elseif -pos > len then return 0
		else return len + pos + 1
		end
	end

	local function check_capture(ms, l)
		l = l - '1'
		if l < 0 or l >= ms.level or ms.capture[l].len == CAP_UNFINISHED then
			error("invalid capture index %" .. (l + 1))
		end
		return l
	end

	local function capture_to_close(ms)
		local level = ms.level
		while level > 0 do
			level = level - 1
			if ms.capture[level].len == CAP_UNFINISHED then
				return level
			end
		end
		return error("invalid pattern capture")
	end

	local function classend(ms, p)
		local p0 = p:char() p = p:copy(1)
		if p0 == L_ESC then
			if p == ms.p_end then
				error("malformed pattern (ends with %)")
			end
			return p:step(1)
		elseif p0 == '[' then
			if p:char() == '^' then
				p:step()
			end
			repeat  -- look for a `]'
				if p == ms.p_end then
					error("malformed pattern (missing ])")
				end
				p:step()
				if p:char(-1) == L_ESC then
					if p < ms.p_end then
						p:step()  -- skip escapes (e.g. `%]')
					end
				end
			until p:char() == ']'
			return p:step()
		else
			return p
		end
	end

	local function match_class(c, cl)
		local res
		local cll = string_lower(cl)
		if cll == 'a' then res = isalpha(c)
		elseif cll == 'c' then res = iscntrl(c)
		elseif cll == 'd' then res = isdigit(c)
		elseif cll == 'g' then res = isgraph(c)
		elseif cll == 'l' then res = islower(c)
		elseif cll == 'p' then res = ispunct(c)
		elseif cll == 's' then res = isspace(c)
		elseif cll == 'u' then res = isupper(c)
		elseif cll == 'w' then res = isalnum(c)
		elseif cll == 'x' then res = isxdigit(c)
		elseif cll == 'z' then res = c == '\0'  -- deprecated option
		else return cl == c
		end
		if islower(cl) then return res
		else return not res
		end
	end

	local function matchbracketclass(c, p, ec)
		local sig = true
		p = p:copy(1)
		if p:char() == '^' then
			sig = false
			p:step()  -- skip the `^'
		end
		while p < ec do
			if p:char() == L_ESC then
				p:step()
				if match_class(c, p:char()) then
					return sig
				end
			elseif p:char(1) == '-' and p + 2 < ec then
				p:step(2)
				if p:char(-2) <= c and c <= p:char() then
					return sig
				end
			elseif p:char() == c then
				return sig
			end
			p:step()
		end
		return not sig
	end

	local function singlematch(ms, s, p, ep)
		if s >= ms.src_end then
			return false
		end
		local p0 = p:char()
		if p0 == '.' then return true -- matches any char
		elseif p0 == L_ESC then return match_class(s:char(), p:char(1))
		elseif p0 == '[' then return matchbracketclass(s:char(), p, ep:copy(-1))
		else return p:char() == s:char()
		end
	end

	local function matchbalance(ms, s, p)
		if p >= ms.p_end - 1 then
			error("malformed pattern (missing arguments to %b)")
		end
		if s:char() ~= p:char() then return nil end
		local b = p:char()
		local e = p:char(1)
		local cont = 1
		s = s:copy()
		while s:step() < ms.src_end do
			if s:char() == e then
				cont = cont - 1
				if cont == 0 then return s:step() end
			elseif s:char() == b then
				cont = cont + 1
			end
		end
		return nil  -- string ends out of balance
	end

	local function max_expand(ms, s, p, ep)
		local i = 0  -- counts maximum expand for item
		while singlematch(ms, s:copy(i), p, ep) do
			i = i + 1
		end
		-- keeps trying to match with the maximum repetitions
		while i >= 0 do
			local res = match(ms, s:copy(i), ep:copy(1))
			if res then return res end
			i = i - 1  -- else didn't match; reduce 1 repetition to try again
		end
		return nil
	end

	local function min_expand(ms, s, p, ep)
		s = s:copy()
		while true do
			local res = match(ms, s, ep:copy(1))
			if res ~= nil then
				return res
			elseif singlematch(ms, s, p, ep) then
				s:step()  -- try with one more repetition
			else return nil
			end
		end
	end

	local function start_capture(ms, s, p, what)
		local level = ms.level
		ms.capture[level] = ms.capture[level] or {}
		ms.capture[level].init = s:copy()
		ms.capture[level].len = what
		ms.level = level + 1
		local res = match(ms, s, p)
		if res == nil then  -- match failed?
			ms.level = ms.level - 1  -- undo capture
		end
		return res
	end

	local function end_capture(ms, s, p)
		local l = capture_to_close(ms)
		ms.capture[l].len = s - ms.capture[l].init  -- close capture
		local res = match(ms, s, p)
		if res == nil then  -- match failed?
			ms.capture[l].len = CAP_UNFINISHED  -- undo capture
		end
		return res
	end

	local function match_capture(ms, s, l)
		l = check_capture(ms, l)
		local len = ms.capture[l].len
		if ms.src_end - s >= len and
			 ms.capture[l].init:head(len) == s:head(len)
		then
			return s:copy(len)
		else return nil
		end
	end

	function match(ms, s, p)
		s = s:copy()
		p = p:copy()
		::init:: -- using goto's to optimize tail recursion
		if p ~= ms.p_end then
			local p0 = p:char()
			if p0 == '(' then  -- start capture
				if p:char(1) == ')' then  -- position capture?
					s = start_capture(ms, s, p:copy(2), CAP_POSITION)
				else
					s = start_capture(ms, s, p:copy(1), CAP_UNFINISHED)
				end
				goto brk
			elseif p0 == ')' then  -- end capture
				s = end_capture(ms, s, p:copy(1))
				goto brk
			elseif p0 == '$' then
				if p + 1 ~= ms.p_end then  -- is the `$' the last char in pattern?
					goto dflt  -- no; go to default
				end
				s = (s == ms.src_end) and s or nil  -- check end of string
				goto brk
			elseif p0 == L_ESC then  -- escaped sequences not in the format class[*+?-]?
				local p1 = p:char(1)
				if p1 == 'b' then  -- balanced string?
					s = matchbalance(ms, s, p:copy(2))
					if s ~= nil then
						p:step(4)
						goto init  -- return match(ms, s, p + 4)
					end
					-- else fail (s == nil)
				elseif p1 == 'f' then  -- frontier?
					p:step(2)
					if p:char() ~= '[' then
						error("missing [ after %f in pattern")
					end
					local ep = classend(ms, p)  -- points to what is next
					local previous = (s == ms.src_init) and '\0' or s:char(-1)
					if not matchbracketclass(previous, p, ep:copy(-1)) and
						 matchbracketclass(s:char(), p, ep:copy(-1))
					then
						p = ep
						goto init  -- return match(ms, s, ep)
					end
					s = nil  -- match failed
				elseif isdigit(p:char(1)) then  -- capture results (%0-%9)?
					s = match_capture(ms, s, p:char(1))
					if s ~= nil then
						p:step(2)
						goto init  -- return match(ms, s, p + 2)
					end
				else
					goto dflt
				end
				goto brk
			end
			::dflt:: do
				local ep = classend(ms, p)  -- points to what is next
				local ep0 = ep:char()
				if not singlematch(ms, s, p, ep) then
					if ep0 == '*' or ep0 == '?' or ep0 == '-' then  -- accept empty?
						p = ep:copy(1)
						goto init  -- return match(ms, s, ep + 1)
					else  -- '+' or no suffix
						s = nil  -- fail
					end
				else  -- matched once
					if ep0 == '?' then  -- optional
						local res = match(ms, s:copy(1), ep:copy(1))
						if res ~= nil then
							s = res
						else
							p = ep:copy(1)
							goto init  -- else return match(ms, s, ep + 1)
						end
					elseif ep0 == '+' then  -- 1 or more repetitions
						s = max_expand(ms, s:copy(1), p, ep)  -- 1 match already done
					elseif ep0 == '*' then  -- 0 or more repetitions
						s = max_expand(ms, s, p, ep)
					elseif ep0 == '-' then  -- 0 or more repetitions (minimum)
						s = min_expand(ms, s, p, ep)
					else
						s:step()
						p = ep
						goto init  -- else return match(ms, s+1, ep);
					end
				end
			end
			::brk::
		end
		return s
	end

	local function push_onecapture(ms, i, s, e)
		if i >= ms.level then
			if i == 0 then  -- ms->level == 0, too
				return s:head(e - s)  -- add whole match
			else
				error("invalid capture index")
			end
		else
			local l = ms.capture[i].len;
			if l == CAP_UNFINISHED then error("unfinished capture") end
			if l == CAP_POSITION then
				return ms.capture[i].init - ms.src_init + 1
			else
				return ms.capture[i].init:head(l)
			end
		end
	end

	local function push_captures(ms, s, e)
		local nlevels = (ms.level == 0 and s) and 1 or ms.level
		local captures = {}
		for i = 0, nlevels - 1 do
			table.insert(captures, push_onecapture(ms, i, s, e))
		end
		return table.unpack(captures)
	end

	-- check whether pattern has no special characters
	local function nospecials(p)
		for i = 1, #p do
			for j = 1, #SPECIALS do
				if p:sub(i, i) == SPECIALS:sub(j, j) then
					return false
				end
			end
		end
		return true
	end

	local function str_find_aux(str, pattern, init, plain, find)
		checkArg(1, str, "string")
		checkArg(2, pattern, "string")
		checkArg(3, init, "number", "nil")

		if #str < SHORT_STRING then
			return (find and string_find or string_match)(str, pattern, init, plain)
		end

		local s = strptr(str)
		local p = strptr(pattern)
		local init = posrelat(init or 1, #str)
		if init < 1 then init = 1
		elseif init > #str + 1 then  -- start after string's end?
			return nil  -- cannot find anything
		end
		-- explicit request or no special characters?
		if find and (plain or nospecials(pattern)) then
			-- do a plain search
			local s2 = string_find(str, pattern, init, true)
			if s2 then
				return s2-s.pos + 1, s2 - s.pos + p:len()
			end
		else
			local s1 = s:copy(init - 1)
			local anchor = p:char() == '^'
			if anchor then p:step() end
			local ms = {
				src_init = s,
				src_end = s:copy(s:len()),
				p_end = p:copy(p:len()),
				capture = {}
			}
			repeat
				ms.level = 0
				local res = match(ms, s1, p)
				if res ~= nil then
					if find then
						return s1.pos - s.pos + 1, res.pos - s.pos, push_captures(ms, nil, nil)
					else
						return push_captures(ms, s1, res)
					end
				end
			until s1:step() > ms.src_end or anchor
		end
		return nil  -- not found
	end

	local function str_find(s, pattern, init, plain)
		return str_find_aux(s, pattern, init, plain, true)
	end

	local function str_match(s, pattern, init)
		return str_find_aux(s, pattern, init, false, false)
	end

	local function str_gmatch(s, pattern, init)
		checkArg(1, s, "string")
		checkArg(2, pattern, "string")

		if #s < SHORT_STRING then
			return string_gmatch(s, pattern, init)
		end

		local start = 0
		if isLuaOver54 then
			checkArg(3, init, "number", "nil")
			if init ~= nil then
				start = posrelat(init, #s)
				if start < 1 then start = 0
				elseif start > #s + 1 then start = #s + 1
				else start = start - 1 end
			end
		end

		local s = strptr(s)
		local p = strptr(pattern)
		return function()
			local ms = {
				src_init = s,
				src_end = s:copy(s:len()),
				p_end = p:copy(p:len()),
				capture = {}
			}
			for offset = start, ms.src_end.pos - 1 do
				local src = s:copy(offset)
				ms.level = 0
				local e = match(ms, src, p)
				if e ~= nil then
					local newstart = e - s
					if e == src then newstart = newstart + 1 end -- empty match? go at least one position
					start = newstart
					return push_captures(ms, src, e)
				end
			end
			return nil  -- not found
		end
	end

	local function add_s(ms, b, s, e, r)
		local news = tostring(r)
		local i = 1
		while i <= #news do
			if news:sub(i, i) ~= L_ESC then
				b = b .. news:sub(i, i)
			else
				i = i + 1  -- skip ESC
				if not isdigit(news:sub(i, i)) then
					b = b .. news:sub(i, i)
				elseif news:sub(i, i) == '0' then
					b = b .. s:head(e - s)
				else
					b = b .. push_onecapture(ms, news:sub(i, i) - '1', s, e)  -- add capture to accumulated result
				end
			end
			i = i + 1
		end
		return b
	end

	local function add_value(ms, b, s, e, r, tr)
		local res
		if tr == "function" then
			res = r(push_captures(ms, s, e))
		elseif tr == "table" then
			res = r[push_onecapture(ms, 0, s, e)]
		else  -- LUA_TNUMBER or LUA_TSTRING
			return add_s(ms, b, s, e, r)
		end
		if not res then  -- nil or false?
			res = s:head(e - s)  -- keep original text
		elseif type(res) ~= "string" and type(res) ~= "number" then
			error("invalid replacement value (a "..type(res)..")")
		end
		return b .. res  -- add result to accumulator
	end

	local function str_gsub(s, pattern, repl, n)
		checkArg(1, s, "string")
		checkArg(2, pattern, "string", "number")
		checkArg(3, repl, "number", "string", "function", "table")
		checkArg(4, n, "number", "nil")

		if #s < SHORT_STRING then
			return string_gsub(s, pattern, repl, n)
		end

		pattern = tostring(pattern)
		local src = strptr(s);
		local p = strptr(pattern)
		local tr = type(repl)
		local max_s = n or (#s + 1)
		local anchor = p:char() == '^'
		if anchor then
			p:step()  -- skip anchor character
		end
		n = 0
		local b = ""
		local ms = {
			src_init = src:copy(),
			src_end = src:copy(src:len()),
			p_end = p:copy(p:len()),
			capture = {}
		}
		while n < max_s do
			ms.level = 0
			local e = match(ms, src, p)
			if e then
				n = n + 1
				b = add_value(ms, b, src, e, repl, tr)
			end
			if e and e > src then  -- non empty match?
				src = e  -- skip it
			elseif src < ms.src_end then
				b = b .. src:char()
				src:step()
			else break
			end
			if anchor then break end
		end
		b = b .. src:head()
		return b, n  -- number of substitutions
	end

	string.find = str_find
	string.match = str_match
	string.gmatch = str_gmatch
	string.gsub = str_gsub
end

local preempt = computer.preempt
local set_thd = computer.set_current_thread
local thd_resume = computer.thd_resume
local yield = computer.int_yield
computer.preempt = nil
computer.set_thd = nil
computer.thd_resume = nil
computer.int_yield = nil

-- wrap coro library
local coro = coroutine
local cr = {}

function cr.resume(co, ...)
	local rtv = table.pack(thd_resume(co, ...))
	while preempt() do -- Yields the current coroutine if the child one was yielded
		cr.yield()
		thd_resume(co) -- resumes it once the parent one has resumed
	end
	return table.unpack(rtv)
end

function cr.kresume(co, ...)
	local rtv = table.pack(thd_resume(co, ...))
	if not preempt() then -- if this isn't a preempt yield, the values are valid
		return table.unpack(rtv)
	else
		yield() -- really should have something else for this
	end
end

function cr.create(fun)
	return coro.create(fun)
end

function cr.wrap(fun)
	local co = cr.create()
	return function(...)
		return cr.resume(co, ...)
	end
end

for k, v in pairs(coro) do
	if not cr[k] then cr[k] = v end
end

coroutine = cr
os = {
	clock = os.clock,
	date = os.date,
	difftime = os.difftime,
	time = os.time
}

math.randomseed = nil

debug = {
	traceback = debug.traceback
}

local rare_fox = computer.rare_fox
computer.rare_fox = nil

xpcall(function()
	print("yerp")
	local tty = computer.tty()
	--computer.set_mem_baseline()
	computer.set_mem_baseline = nil
	local bios = load(computer.eeprom():code(), "=bios.lua")
	print("yerp 2")
	computer.pull_signal()
	bios()
	error("halted")
end, function(err)
	-- print to vt
	local tty = computer.tty()
	if tty then
		tty:write(debug.traceback(err):gsub("\n","\r\n"))
		tty:write("\r\n")
		tty:write("\27[2;60H")
		tty:write(rare_fox())
		 tty:write("\27[;60H Crashes are rare")
		tty:write("\27[9;60H  As is this fox")
	end
end)