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

print("Testing yield value preservation")
local vals = {
  "a",
  "b",
  "c",
}
local function test2()
  for i, v in ipairs(vals) do
    if vals[i+1] then
      coroutine.yield(v)
    else
      return v
    end
  end
end
local co = coroutine.create(test2)
print("coro is "..tostring(co))
local failed = false
for i, expect in ipairs(vals) do
  local ok, real = coroutine.resume(co)
  if not ok then
    log.error(string.format("FAIL: coroutine error: %s", real))
  elseif real ~= expect then
    log.error(("%u: FAIL! Expected %s, got %s"):format(i, expect, real))
    failed = true
  else
    log.ok(("%u: OK!"):format(i))
  end
end
if failed then
  test_fail("Failures in yield value preservation, see above")
else
  log.ok("OK!")
end