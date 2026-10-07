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

local function find_cmd(cmdname)
    local path = os.getenv("path")
    path = path or {"/bin/?.lua"}
    for i=1, #path do
        local cpath = path[i]:gsub("%?", cmdname)
        if io.exists(cpath) then
            return cpath
        end
    end
end

local function cmd_call(self, arg)
    if not arg then
        -- actually run the command list
        local node = self
        while node.parent do
            if node.parent.error then
                io.stderr:writeln(string.format("slash: %s", node.parent.error))
                return
            end
            print(node.cmd)
            node = node.parent
        end
    end
    table.insert(self.args, arg)
    return self
end

cenv.__call = cmd_call

local function cmd_index(self, index)
    local cmd = find_cmd(index)
    if not cmd then
        return {
            error = string.format("command not found: %s", cmd)
        }
    end
    local node = cmd_node(cmd)
    node.parent = self
    self.child = node
    return node
end

cenv.__index = cmd_index

function shell.compile(statement)
    
end

return shell