local function l54_frexp(x)
    checkArg(1, x, "number")
    if x == 0 then
        return 0, 0
    end
    local bits = string.unpack("l", (string.pack("d", x))) -- magic pure lua casting function
    -- extract sign bit
    local sign = bits & (1 << 63)
    -- exponent is bits 52 to 62, and it's offset by 1023.
    local exp_ = ((bits >> 52) & 0x7FF)
    local exp = exp_ - 1023
    -- handle special cases
    if exp_ == 0x7FF then
        return x, exp+1
    end
    -- bits 0 to 51 are the mantissa
    local mant_bits = bits & 0xFFFFFFFFFFFFF
    -- we then wanna normalize the mantissa and put the sign bit back on.
    local mant = string.unpack("d", (string.pack("l", mant_bits | 0x3FE0000000000000 | sign)))
    -- increase exponent by 1 and return
    return mant, exp+1
end