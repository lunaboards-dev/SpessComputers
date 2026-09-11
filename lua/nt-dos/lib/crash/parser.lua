local parser = {}

local reader = {}
function reader:next(n)
    n = n or 1
    local dat = self:peek(n)
    self:seek(n)
    return dat
end

function reader:peek(n)
    n = n or 1
    self.dat:sub(self.hand, self.hand+n-1)
end

function reader:seek(n)
    n = n or 0
    self.hand = self.hand + n
    return self.hand
end

function reader:eof()
    return self.hand > #self.hand
end

function reader:valid()
    return not self:eof() and self.hand > 0
end

local lit_escapes = {
    a = "\a",
    b = "\b",
    f = "\f",
    n = "\n",
    r = "\r",
    t = "\t",
    ["'"] = "'",
    ['"'] = '"',
    ["\\"] = "\\"
}

function parser:read_string(close, attr)
    local parts = {}
    local function append(str)
        local p = parts[#parts]
        p.data = p.data .. str
    end
    local function add_token(tok)
        table.insert(parts, tok)
        table.insert(parts, {
            type = "literal",
            data = ""
        })
    end
    while true do
        local c = self.reader:next()
        local peeked = self.reader:peek(#close-2) or ""
        if not attr.lit and c == "\\" then
            local esc = self.reader:next()
            local le = lit_escapes[esc]
            if le then
                append(le)
            elseif esc >= "0" and esc <= "9" then
                append(string.char(tonumber(esc, 10)))
            elseif esc == "x" then
                local hex = self.reader:next(2)
                local n = tonumber(hex, 16)
                if not n then
                    self:error("malformed hex escape")
                end
                append(string.char(n))
            elseif esc == "u" then
                local hex = self.reader:next(4)
                local n = tonumber(hex, 16)
                if not n then
                    self:error("malformed unicode escape")
                end
            elseif esc == "{" and attr.pat then
                local tok = self:next_token({stat=true, lit=true})
                add_token({type="sub", tok = tok})
            end
        elseif c..peeked == close then
            self.reader:skip(peeked)
            break
        else
            append(c)
        end
    end
    return {
        type = "string",
        parts = parts
    }
end

function parser:read_number()
    local buffer = ""
    while true do
        local c = self.reader:peek()
        if not c or c:match("[%s,;]") then break end
        self.reader:skip()
        buffer = buffer .. c
    end
    local pfx = buffer:sub(1, 2):lower()
    local val = buffer:sub(3):gsub("_", "")
    local v
    if pfx == "0b" then
        v = tonumber(val, 2)
    elseif pfx == "0o" then
        v = tonumber(val, 8)
    else -- use lua's built in number parsing, though strip out underscores
        buffer = buffer:gsub("_", "")
        v = tonumber(buffer)
    end
    if not v then
        self:error("malformed number")
    end
    return {
        type = "number",
        value = v
    }
end

return parser