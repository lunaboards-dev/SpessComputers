local fs = {}

---@alias mount_proxy {
---open:function,
---pstat:function,
---rstat:function,
---opendir:function,
---used:function,
---size:function,
---readline:function}
---@alias mount {point: string[], proxy: mount_proxy}

---@type mount[]
local mounts = {}

fs.mounts = mounts

---Splits path into parts
---@param path string Path to split
---@return string[] # Path parts
function fs.path(path)
    ---@type string[]
    local parts = {}
    for part in path:gmatch("[^/]+") do
        if part == ".." then
            table.remove(parts)
        elseif part ~= "." then
            table.insert(parts, part)
        end
    end
---@diagnostic disable-next-line: inject-field
    parts.path = table.concat(parts, "/")
    return parts
end

-- For consistant resolving
local function mount_sort(a, b)
    return (#a.point == #b.point) and (a.path < b.path) or (#a.point > #b.point)
end

function fs.mount(point, proxy)
    local mountpoint = fs.path(point)
    table.insert(mounts, {
        point = mountpoint,
        proxy = proxy
    })
    table.sort(mounts, mount_sort)
end

function fs.unmount(point)
    if type(point) == "string" then
        point = fs.path(point)
    end
    for i=1, #mounts do
---@diagnostic disable-next-line: undefined-field
        if mounts[i].point.path == point.path then
            table.remove(mounts[i])
            return
        end
    end
end

local function path_pfx_match(pfx, path)
    for i=1, #path do
        if pfx[i] ~= path[i] then return false end
    end
    return true
end

local function unprefix(pfx, path)
    local upfx = {}
    for i=#pfx+1, #path do
        table.insert(upfx, path[i])
    end
    return table.concat(upfx, "/")
end

function fs.resolve(path)
    local spath = fs.path(path)
    for i=1, #mounts do
        if path_pfx_match(mounts[i].point, spath) then
            return mounts[i].proxy, unprefix(mounts[i].point, spath)
        end
    end
end

function fs.resolve_if_exists(path)
    local prox, path = fs.resolve(path)
    if prox:pstat(path) then
        return prox, path
    end
end

return fs