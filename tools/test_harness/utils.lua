local utils = {}

function utils.concat(c, ...)
    local t = table.pack(...)
    for i=1, t.n do
        t[i] = tostring(t[i])
    end
    return table.concat(t, c)
end

function utils.panic(...)
    io.stderr:write("PANIC!: ", utils.concat("\t", ...), "\n")
    os.exit(1)
end

function utils.bwoink(...)
    io.stderr:write("BWOINK!: ", utils.concat("\t", ...), "\n")
end

return utils