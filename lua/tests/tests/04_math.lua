local function test_frexp(n, m1, x1)
        local m2, x2 = math.frexp(n)
        print("Test frexp: "..n)
        print(string.format("frexp:          %q\t%q", m1, x1))
        print(string.format("pure lua frexp: %q\t%q", m2, x2))
        local mc = m1 == m2 or (m1 ~= m1 and m2 ~= m2)
        local xc = x1 == x2
        local er = {}
        if not mc then table.insert(er, "mantissas don't match") end
        if not xc then table.insert(er, "exponents don't match") end
        if mc and xc then
                log.ok("PASS!")
        else
                test_fail("FAIL ("..table.concat(er, ", ")..")")
        end
end

test_frexp(0, 0x0p+0, 0)
test_frexp(10.54, 0x1.5147ae147ae14p-1, 4)
test_frexp(1934398493, 0x1.cd325074p-1, 31)
test_frexp(1e9999, 1e9999, 0)
test_frexp(-1e9999, -1e9999, 0)
test_frexp((0/0), (0/0), 0)