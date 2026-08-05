--#include "vcheck.lua"
--#include "utils.lua"
--#include "patterns.lua"
--#include "peripheral.lua"
--#include "coro.lua"
--#include "frexp.lua"
--#include "env.lua"

ypcall(function()
    local bios = assert(sandbox.load(peripheral.call(computer.eeprom(), "code"), "=bios"))
    for i=1, 10 do
        yield()
        collectgarbage()
    end
    computer.set_mem_baseline()
    gatekeeper(bios)
end, function(err, trace)
    -- print to vt
    local tty = computer.tty()
    if tty then
        --print(debug.traceback(err))
        tty_writeln(trace:gsub("\n","\r\n"))
        tty_writeln("\r\n")
        tty_write("\27[2;60H")
        tty_writeln(rare_fox())
        tty_writeln("\27[;60H Crashes are rare")
        tty_writeln("\27[9;60H  As is this fox")
    end
end)