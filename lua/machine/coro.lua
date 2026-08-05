local preempt = system.preempt
local set_thd = system.set_current_thread
local thd_resume = system.thd_resume
local yield = system.int_yield
local is_iores = system.is_iores

-- wrap coro library
local coro = coroutine
local cr = {}

function cr.resume(co, ...)
	local rtv = table.pack(thd_resume(co, ...))
	while preempt() do -- Yields the current coroutine if the child one was yielded
		local rtv = table.pack(cr.yield())
		thd_resume(co, table.unpack(rtv)) -- resumes it once the parent one has resumed
	end
	return table.unpack(rtv)
end

function cr.kresume(co, ...)
	local rtv = table.pack(thd_resume(co, ...))
	while is_iores() do
		rtv = table.pack(thd_resume(co, rtv))
	end
	if not preempt() then -- if this isn't a preempt yield, the values are valid
		return table.unpack(rtv)
	else
		yield()
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

--coroutine = cr