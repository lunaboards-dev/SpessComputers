local function sixwrap(six)
    print("\27Pq\n"..six.."\n\27\\")
end

sixwrap [[
#0;2;0;0;0#1;2;100;100;0#2;2;0;100;0
#1~~@@vv@@~~@@~~$
#2??}}GG}}??}}??-
#1!14@
]]

print("\r\n^ Should say 'hi'")

local eeprom = peripheral.proxy(computer.eeprom())

eeprom:cfg_set("sc_test_foo", "bar")
assert(eeprom:cfg_get("sc_test_foo") == "bar")
log.ok("EEPROM config writing works.")