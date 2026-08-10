local vfs = {
    f_read = 1,
    f_write = 2,
    f_exec = 4,
    s_user = 0,
    s_group = 3,
    s_other = 6,
    f_suid = 1 << 9,
    f_sgid = 1 << 10,
    m_rwx = 7,
    of_read = 1,
    of_write = 2,
    of_create = 4,
    of_truncate = 8
}

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
            path = mount.prefix
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
    local mounts = vfs.resolve_path(cpath)
    local idx = 0
    local iter
    local parent = 0
    local function rtf()
        if not mounts[idx] then return end
        if not iter then
            iter = mounts[idx].path:gmatch("[^/]+")
            parent = 0
        end
        local rtv = iter()
        if not rtv then idx = idx+1 return rtf() end
        local st = mounts[idx].fs.fs:rstat(parent, rtv)
        parent = st.inode
        return rtv, st
    end
    return rtf
end

function vfs.test_permissions(path, op, uid, groups)
    for name, stat in vfs.walk_path(path) do
        local r_flags = op
        r_flags = r_flags & (((stat.flags >> vfs.s_other) & vfs.m_rwx) ~ vfs.m_rwx)
        for i=1, #groups do
            if stat.ogroup == groups[i] then
                r_flags = r_flags & (((stat.flags >> vfs.s_group) & vfs.m_rwx) ~ vfs.m_rwx)
                break
            end
        end
        if stat.owner == uid then
            r_flags = r_flags & ((stat.flags & vfs.m_rwx) ~ vfs.m_rwx)
        end
        if r_flags ~= 0 then return false end
    end
    return true
end

return vfs