local env = {}

local env_trk = {}

local function env_set(ctx, var, val)

end

local function env_get(ctx, var)

end

local function make_ctx(euid, egids)
    local ctx = {}

    function ctx.load(chunk, name)

    end

    return ctx
end

local ectx = {}
local lctx = {}

function ectx:euid()

end

function ectx:egids()
    
end

function ectx:set(var, val)

end

function ectx:get(var)

end

function ectx:clone()

end

function ectx:new_child()

end

function lctx:new_child()

end

function env.new(parent, opt)
    return setmetatable({
        vars = {},
        packages = {}
    })
end

return env