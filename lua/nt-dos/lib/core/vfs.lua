local vfs = {}

local hand = {}

local mounts = {}

local function sort_mounts()
    table.sort(mounts, function(a, b)
        local a_c = select(2, a.prefix:gsub("/", ""))
        local b_c = select(2, b.prefix:gsub("/", ""))
        if a_c == b_c then return a < b end
        return a_c > b_c
    end)
end

--- Returns a canonical path
function vfs.canonical(path)
    local parts = {}
    for part in path:gmatch("[^/]+") do
        if part == ".." then
            table.remove(parts)
        elseif part ~= "." then
            table.insert(parts, part)
        end
    end
    return "/"..table.concat(parts, "/")
end

function vfs.resolve_path(path)
    local mount_xing = {}
    for i=1, #mounts do
        local mount = mounts[i]
        local path_pfx = mount.prefix .. "/"
        if path:sub(1, #path_pfx) == path_pfx then
            table.insert(mount_xing, 1, {
                path = path:sub(#path_pfx+1),
                fs = mount
            })
        end
    end
    return mount_xing
end

--- Resolves a path to its filesystem object and relative path.
---@param path string Path to resolve
---@return table? fs Filesystem object or nil
---@return string rpath Relative path or error
function vfs.resolve(path)
    for i=1, #mounts do
        local mount = mounts[i]
        local path_pfx = mount.prefix .. "/"
        if path:sub(1, #path_pfx) == path_pfx then
            return mount, path:sub(#path_pfx+1)
        end
    end
    return nil, "not found"
end

function vfs.mount(prefix, fsobj)
    local cpath = vfs.canonical(prefix)
    table.insert(mounts, {
        prefix = cpath,
        fs = fsobj
    })
    sort_mounts()
end

function vfs.walk_path(path)
    local cpath = vfs.canonical(path)
end

function vfs.test_permissions(path, op, uid, ...)

end

return vfs