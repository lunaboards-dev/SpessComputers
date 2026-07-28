local bdev = ...

function fstat(path)
    local parent = {}
    for part in path:gmatch("[^/]+") do
        parent = bdev:query("select * from fmeta where parent=? and name=?;", parent.inode, part):read()
        if not parent then return nil, path..": not found" end
    end
    return parent
end

function readfile(path)
    local res, err = fstat(path)
    if not res then return nil, err end
    local buffer = {}
    for chunk in bdev:query("select data, blkid from fdata where inode=? order by blkid asc;", res.inode):values() do
        table.insert(buffer, chunk)
    end
    return table.concat(buffer):sub(1, res.size)
end

function loadfile(path)
    local dat = assert(readfile(path))
    return load(path, "="..path, "t")
end

function dofile(path)
    return loadfile(path)()
end

function dir(path)
    local res, err = fstat(path)
    if not res then return nil, err end
    return bdev:query("select name from fmeta where parent=? order by name asc;", res.inode):values()
end

for test in dir("tests") do
    dofile("")
end