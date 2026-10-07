local boot = ...
-- duplicated from vfs
local flags = {

}

local fs = {}

function fs:open(path, flags)

end

function fs:pstat(path)
    local parent = {}
    for m in path:gmatch("[^/]+") do
        local st, err = self:rstat(parent.inode, m)
        if not st then return nil, err end
        parent = st
    end
    return parent
end

function fs:rstat(parent, entry)
    local pid
    if parent and parent > 0 then pid = parent end
    local res = self.dev:query("select * from fmeta where name=? and "..(pid and "parent=?" or "parent is null"), table.unpack {entry, pid})
    if res:empty() then return nil, "not found" end
    return res:read()
end

function fs:opendir(path)
    local st, err = self:pstat(path)
    if not st then return nil, err end
    local res
    if not st.inode then
        res = self.dev:query("select name from fmeta where parent is null")
    else
        res = self.dev:query("select name from fmeta where parent=?", st.inode)
    end
    return function()
        return res:next()
    end
end

function fs:used()
    return (self.dev:query("SELECT (page_count - freelist_count) * page_size AS used_bytes FROM pragma_page_count(), pragma_freelist_count(), pragma_page_size();"):next())
end

function fs:size()

end

function fs:readlink(path)
    local st, err = self:pstat(path)
    if not st then return st, err end
    return st.target
end

function fs:mklink(path, target)

end

function fs:mkdir(path)

end

return fs