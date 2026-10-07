local shell = {}

--[[
    examples:
        find "/bin" : filter "%.lua$" : lsub "%.lua$" "" ()
]]

local cenv = {}

local function cmd_node(cmd)
    return setmetatable({
        cmd = cmd,
        args = {}
    }, {__index=cenv.__index, __call=cenv.__call})
end

local function find_cmd(env, cmdname)

end

local function cmd_call(self, arg)
    table.insert(self.args, arg)
    return self
end

cenv.__call = cmd_call

local function cmd_index(self, index)
    local cmd = find_cmd(env, index)
    if not cmd then
        -- handle error or something
    end
    return setmetatable()
end

cenv.__index = cmd_index

function shell.compile(statement)
    
end

return shell