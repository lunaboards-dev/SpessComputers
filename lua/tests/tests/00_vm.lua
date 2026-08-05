if critical then
    log.debug("Debug environment: User defined critical regions enabled!")
    local function test_func(a, b, c)
        return a+b+c
    end
    local crit_func = critical(test_func)
    local res1 = test_func(1, 2, 3)
    log.debug(string.format("test_func: %s", tostring(res1)))
    local res2 = crit_func(1, 2, 3)
    log.debug(string.format("crit_func: %s", tostring(res2)))
    if res1 ~= res2 then
        test_fail(string.format("Critical function return does not match normal return! (%s ~= %s)", tostring(res1), tostring(res2)))
    end
    log.ok(string.format("PASS! %s = %s", tostring(res1), tostring(res2)))
end