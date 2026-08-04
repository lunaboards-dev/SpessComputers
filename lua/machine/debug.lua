local gatekeeper_info = {}

local _debug = debug

local function max_lookup()
    local lvl = 1
    while true do
        local di = _debug.getinfo(lvl, "n")
        if di and di.name == "gatekeeper" then
            return lvl-1
        end
    end
end

local dbg = {
    -- debug is not allowed
    -- gethook is not allowed
    --
}