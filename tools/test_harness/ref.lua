local ref = {}

local refs = setmetatable({}, {__mode="kv"})
local perma = {}

ref.refs = refs
ref.store = perma

function ref.add(obj, store)
    local i = 1
    while not refs[i] do
        i = i + 1
    end
    refs[i] = obj
    refs[obj] = i
    if store then
        perma[obj] = true
    end
end

function ref.get(obj)
    return refs[obj]
end

return ref