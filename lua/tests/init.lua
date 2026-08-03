local _bdev = ...
local bdev = peripheral.proxy(_bdev)

local tty = peripheral.proxy(computer.tty())

local _print = print
function print(str)
    tty:write(str:gsub("\n", "\r\n").."\r\n")
    _print(str)
end

function fstat(path)
    local parent
    for part in path:gmatch("[^/]+") do
        if (parent) then
            parent = bdev:query("select * from fmeta where name = ? and parent = ?", part, parent.inode):read()
        else
            parent = bdev:query("select * from fmeta where name = ? and parent is null", part):read()
        end
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
    return load(dat, "="..path, "t")
end

function dofile(path)
    return loadfile(path)()
end

function dir(path)
    local res, err = fstat(path)
    if not res then return nil, err end
    return bdev:query("select name from fmeta where parent = ? order by name asc;", res.inode):values()
end

print("SpessComputers test suite")

for test in assert(dir("tests")) do
    print(":: Running "..test)
    dofile("tests/"..test)
end

print("Tests passed!")