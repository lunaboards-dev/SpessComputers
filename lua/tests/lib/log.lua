local log = {}

function log.ok(str)
    print("\27[92m"..str.."\27[0m")
end

function log.error(str)
    print("\27[31m"..str.."\27[0m")
end

function log.warn(str)
    print("\27[93m"..str.."\27[0m")
end

function log.debug(str)
    print("\27[90m"..str.."\27[0m")
end

return log