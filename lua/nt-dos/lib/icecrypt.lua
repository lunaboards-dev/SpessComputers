local icrypt = {}
local blake = require("blake2b")
local ice = require("ice")

local function ur_read(amt)
    local f = io.open("/dev/urandom", "rb")
    local d = f:read(amt)
    f:close()
    return d
end

local alphabet = "./0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"

local function tob64(n, len)
    local r = ""
    for i=1, len do
        local b = n & 0x3f
        r = alphabet:sub(b+1, b+1)..r
        n = n >> 6
    end
    return r
end

local function fromb64(s)
    local r = 0
    for i=1, #s do
        local pos = alphabet:find(s:sub(i,i), 1, true)
        r = (r << 6) | (pos-1)
    end
    return r
end

local function b64e(str, nopad)
    local pad = ""
    if #str % 3 ~= 0 then
        local t = string.rep("\0", 3-(#str % 3))
        str = str .. t
        pad = string.rep("%", #t)
    end
    local c, n = "", 0
    local res = ""
    for i=1, #str, 3 do
        c, n = string.unpack(">I3", str, i)
        --[[for j=0, 3 do
            local d = (c >> (j*6)) & 0x3f
            res = res .. alphabet:sub(d+1, d+1)
        end]]
        res = res .. tob64(c, 4)
    end
    return res..(nopad and "" or pad)
end

local function b64d(str, len)
    local buffer = {}
    local pad = select(2, str:gsub("%%", ""))
    for i=1, #str-pad, 4 do
        local part = str:sub(i, i+3)
        local cbuf = fromb64(part)
        local _t = string.pack(">I3", cbuf)
        table.insert(buffer, _t)
    end
    local res = table.concat(buffer)
    if len then
        return res:sub(1, len)
    end
    return res:sub(1, -(pad+1))
end

local magic1 = "According2AllKnownLawsOfAviation"
local magic2 = 0x4206980081351337

local function preproc(a, b, offset)
    local v = string.unpack("l", b, offset) ~ a
    return string.pack("l", v), v
end

local function xor_stage(a, b, offset)
    local v1 = string.unpack("l", b, offset)
    return string.pack("l", v1 ~ a), v1
end

local function hex(res)
    return string.format(string.rep("%.2x", #res), res:byte(1, #res))
end

function icrypt.hash(password, level)
    local salt = ur_read(16)
    local pwhash = blake.hash(password..salt, math.max(level*8, 8))
    local ik = ice(level)
    ik.setkey(pwhash)
    local last, cnk
    local res = magic1
    for i=1, 256 do
        local nres = ""
        local xor = magic2
        for j=0,3 do
            cnk, xor = xor_stage(xor, res, j*8+1)
            cnk = ik.encrypt(cnk:reverse())
            nres = nres .. cnk
        end
        res = nres
    end
    local pwhash = b64e(res, true)
    return string.format("$i$%s$%s#%d", b64e(salt, true), pwhash, level)
end

function icrypt.verify(hash, password)
    local _salt, _pw, _level = hash:match("^$i$([^$]+)$([^$#]+)#(.)")
    if not _salt then return end
    local salt = b64d(_salt, 16)
    local pwres = b64d(_pw, 32)
    local level = tonumber(_level, 10)
    local pwhash = blake.hash(password..salt, math.max(level*8, 8))
    local ik = ice(level)
    ik.setkey(pwhash)
    for i=1, 256 do
        local xor = magic2
        local nres = ""
        for j=0,3 do
            local cnk = ik.decrypt(pwres:sub(j*8+1, j*8+8)):reverse()
            local v = string.unpack("l", cnk) ~ xor
            cnk = string.pack("l", v)
            xor = v
            nres = nres .. cnk
        end
        pwres = nres
    end
    --print(pwres)
    return pwres == magic1
end

icrypt.b64e = b64e
icrypt.b64d = b64d

return icrypt
