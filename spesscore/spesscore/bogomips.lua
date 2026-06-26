-- Taken from OC's machine.lua
local hookInterval = 100
local function calcHookInterval()
	local bogomipsDivider = 0.5
	local bogomipsDeadline = os.clock() + bogomipsDivider
	local ipsCount = 0
	local bogomipsBusy = true
	local function calcBogoMips()
		ipsCount = ipsCount + hookInterval
		if os.clock() > bogomipsDeadline then
			bogomipsBusy = false
		end
	end
	-- The following is a bit of nonsensical-seeming code attempting
	-- to cover Lua's VM sufficiently for the IPS calculation.
	local bogomipsTmpA = {{["b"]=3, ["d"]=9}}
	local function c(k)
		if k <= 2 then
			bogomipsTmpA[1].d = k / 2.0
		end
	end
	debug.sethook(calcBogoMips, "", hookInterval)
	while bogomipsBusy do
		local st = ""
		for k=2,4 do
			st = st .. "a" .. k
			c(k)
			if k >= 3 then
				bogomipsTmpA[1].b = bogomipsTmpA[1].b * (k ^ k)
			end
		end
	end
	debug.sethook()
	return ipsCount / bogomipsDivider
end

local ipsCount = calcHookInterval()
-- Since our IPS might still be too generous (hookInterval needs to run at most
-- every 0.05 seconds), we divide it further by 10 relative to that.
hookInterval = (ipsCount * 0.001)
--if hookInterval < 1000 then hookInterval = 1000 end
print(hookInterval)

_HOOKINT = hookInterval