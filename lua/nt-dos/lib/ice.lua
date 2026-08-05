-- ICE Encryption in pure lua (5.3)
-- Don't expect me to make a 5.2 release.

local sunpack, mmax, schar, tunpack = string.unpack, math.max, string.char, table.unpack

local sbox = {{},{},{},{}}
local smod = {
	{333, 313, 505, 369},
	{379, 375, 319, 391},
	{361, 445, 451, 397},
	{397, 425, 395, 505}
}

local sxor = {
	{0x83, 0x85, 0x9b, 0xcd},
	{0xcc, 0xa7, 0xad, 0x41},
	{0x4b, 0x2e, 0xd4, 0x33},
	{0xea, 0xcb, 0x2e, 0x04}
}

local pbox = {
	0x00000001, 0x00000080, 0x00000400, 0x00002000,
	0x00080000, 0x00200000, 0x01000000, 0x40000000,
	0x00000008, 0x00000020, 0x00000100, 0x00004000,
	0x00010000, 0x00800000, 0x04000000, 0x20000000,
	0x00000004, 0x00000010, 0x00000200, 0x00008000,
	0x00020000, 0x00400000, 0x08000000, 0x10000000,
	0x00000002, 0x00000040, 0x00000800, 0x00001000,
	0x00040000, 0x00100000, 0x02000000, 0x80000000
}

local keyrot = {
	0, 1, 2, 3, 2, 1, 3, 0,
	1, 3, 2, 0, 3, 1, 0, 2
}

local function gf_mult(a, b, m)
	local res = 0
	while (b ~= 0) do
		if (b & 1 ~= 0) then
			res = res ~ a
		end
		a = (a << 1) & 0xFFFFFFFF
		b = b >> 1
		if (a >= 256) then
			a = a ~ m
		end
	end
	return res
end

local function gf_exp7(b, m)
	local x
	if (b == 0) then
		return 0
	end
	x = gf_mult(b, b, m)
	x = gf_mult(b, x, m)
	x = gf_mult(x, x, m)
	return gf_mult(b, x, m)
end

local function ice_perm32(x)
	local res = 0
	local pbox_loc = 1
	while (x ~= 0) do
		if (x & 1 ~= 0) then
			res = res | pbox[pbox_loc]
		end
		pbox_loc = pbox_loc + 1
		x = x >> 1
	end
	return res
end

local function ice_sboxes_init()
	for i=1, 1024 do
        local cr = i-1
		local col = (cr >> 1) & 0xFF
		local row = ((cr & 1) | ((cr & 0x200) >> 8)) + 1
		local x
		x = gf_exp7(col ~ sxor[1][row], smod[1][row]) << 24
		sbox[1][i] = ice_perm32(x)
		x = gf_exp7(col ~ sxor[2][row], smod[2][row]) << 16
		sbox[2][i] = ice_perm32(x)
		x = gf_exp7(col ~ sxor[3][row], smod[3][row]) << 8
		sbox[3][i] = ice_perm32(x)
		x = gf_exp7(col ~ sxor[4][row], smod[4][row])
		sbox[4][i] = ice_perm32(x)
	end
end

local function ice_key_create(n)
	local ik = {}
	if (n < 1) then
		ik.size = 1
		ik.rounds = 8
	else
		ik.size = n
		ik.rounds = n * 16
	end
	ik.keysched = {}
	for i=1, mmax(n*16, 8) do
		ik.keysched[i] = {0,0,0}
	end
	return ik
end

local function ice_key_destroy(ik)
	-- Lua has a GC. No manual freeing.
end

local function ice_f(p, sk)
	local tl, tr
	local al, ar
	tl = ((p >> 16) & 0x3ff) | (((p >> 14) | (p << 18)) & 0xffc00)
	tr = (p & 0x3ff) | ((p << 2) & 0xffc00)

    --print(string.format("tl, tr: %x %x", tl, tr))

	al = sk[3] & (tl ~ tr)
	ar = al ~ tr
	al = al ~ tl

	al = al ~ sk[1]
	ar = ar ~ sk[2]

    --print(string.format("al, ar: %x %x", al, ar))

    --[[local s1, s2, s3, s4 = sbox[1][(al >> 10)+1],  sbox[2][(al & 0x3ff)+1],
		sbox[3][(ar >> 10)+1], sbox[4][(ar & 0x3ff)+1] ]]
    
    return sbox[1][(al >> 10)+1] | sbox[2][(al & 0x3ff)+1] |
		sbox[3][(ar >> 10)+1] | sbox[4][(ar & 0x3ff)+1]
	--[[return (
		sbox[1][(al >> 10)+1] | sbox[2][(al & 0x3ff)+1] |
		sbox[3][(ar >> 10)+1] | sbox[4][(ar & 0x3ff)+1]
	)]]
end

local function ice_key_encypt(ik, ptext)
	local i
	local l, r

	--[[l = (ptext:byte(1) << 24) |
		(ptext:byte(2) << 16) |
		(ptext:byte(3) << 8) |
		(ptext:byte(4))

	r = (ptext:byte(5) << 24) |
		(ptext:byte(6) << 16) |
		(ptext:byte(7) << 8) |
		(ptext:byte(8))]]

	l, r = sunpack(">I4I4", ptext)

	local ks = ik.keysched

	for i=1, ik.rounds, 2 do
		l = l ~ ice_f(r, ks[i])
		r = r ~ ice_f(l, ks[i+1])
	end
	local ctext = {}
	for i=0, 3 do
		ctext[4-i] = r & 0xff
		ctext[8-i] = l & 0xff

		r = r >> 8
		l = l >> 8
	end
	return schar(tunpack(ctext))
end

local function ice_key_decrypt(ik, ctext)
	local i
	local l, r

	--[[l = (ctext:byte(1) << 24) |
		(ctext:byte(2) << 16) |
		(ctext:byte(3) << 8) |
		(ctext:byte(4))

	r = (ctext:byte(5) << 24) |
		(ctext:byte(6) << 16) |
		(ctext:byte(7) << 8) |
		(ctext:byte(8))]]

	l, r = sunpack(">I4I4", ctext)
	
	local ks = ik.keysched

	for i=ik.rounds-1, 0, -2 do
		l = l ~ ice_f(r, ks[i+1])
		r = r ~ ice_f(l, ks[i])
	end
	local ptext = {}
	for i=0, 3 do
		ptext[4-i] = r & 0xff
		ptext[8-i] = l & 0xff

		r = r >> 8
		l = l >> 8
	end
	return schar(tunpack(ptext))
end

local function ice_key_sched_build(ik, kb, n, keyrot)
	for i=1, 8 do
		local kr = keyrot[i]
		local isk = ik.keysched[n+i]

		for j=1, 3 do
			isk[j] = 0
		end

		for j=1, 15 do
            local j1 = j-1
			local curr_sk = (j1 % 3)+1
			for k=1, 4 do
                local k1 = k-1
				local curr_kb = ((kr+k1) & 3)+1
				local kbi = kb[curr_kb]
				local bit = kbi & 1
				isk[curr_sk] = (isk[curr_sk] << 1) | bit
				kb[curr_kb] = (kbi >> 1) | ((bit ~ 1) << 15)
			end
		end
	end
end

local function ice_key_set(ik, key)
	if (ik.rounds == 8) then
		local kb = {}
		for i=0, 3 do
			kb[4-i] = sunpack(">H", key, i*2+1) --(key:byte(i*2+1) << 8) | key:byte(i*2+2)
		end

		ice_key_sched_build(ik, kb, 0, keyrot)
		return
	end

	for i=0, ik.size-1 do
		local kb = {}
		for j=0, 3 do
			
			kb[4-j] = sunpack(">H", key, (i*8) + (j*2) + 1)--[[(key:byte((i)*8 + (j)*2 + 1) << 8) |
						key:byte((i)*8 + (j)*2 + 2)]]
		end
		ice_key_sched_build(ik, kb, (i)*8, keyrot)
		local krot = {}
		for j=1, 8 do
			krot[j] = keyrot[8+j]
		end
		ice_key_sched_build(ik, kb, ik.rounds-8-(i)*8, krot)
	end
end

local function ice_key_key_size(ik)
	return ik.size * 8
end

local function ice_key_block_size(ik)
	return 8
end

ice_sboxes_init()

--[[for i=1, #sbox do
    local r = sbox[i]
    io.stdout:write("{")
    for j=1, #r do
        io.stdout:write(string.format("0x%x, ", r[j]))
    end
    io.stdout:write("},\n")
end]]

return function(level)
	local ik = ice_key_create(level)
	return {
		destroy = function()
			ice_key_destroy(ik)
		end,
		keysize = function()
			return ice_key_key_size(ik)
		end,
		blocksize = function()
			return ice_key_block_size(ik)
		end,
		setkey = function(key)
			if (#key ~= ice_key_key_size(ik)) then return nil, "input data size does not match key size" end
			ice_key_set(ik, key)
			return true
		end,
		encrypt = function(dat)
			if (#dat ~= 8) then return nil, "input data size does not match block size" end
			return ice_key_encypt(ik, dat)
		end,
		decrypt = function(dat)
			if (#dat ~= 8) then return nil, "input data size does not match block size" end
			return ice_key_decrypt(ik, dat)
		end,
	}
end
