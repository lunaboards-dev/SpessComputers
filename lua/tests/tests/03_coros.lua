local completed = false
local function count()
    local j = 0
    for i=1, 1000000 do
        j = (j ~ i) << 1
    end
    log.debug("Result: "..j)
    completed = true
end

local function test(func)
    completed = false
    local cr = coroutine.create(count)
    func(cr)
    return completed
end

print("Testing coroutine.resume")
if not test(coroutine.resume) then
    test_fail("FAIL! Function should complete when using coroutine.resume!")
else
    log.ok("OK!")
end
print("Testing coroutine.kresume")
if test(coroutine.kresume) then
    test_fail("FAIL! Function should NOT complete when using coroutine.kresume!")
else
    log.ok("OK!")
end