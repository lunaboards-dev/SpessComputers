local eeprom = peripheral.proxy(computer.eeprom())

eeprom:cfg_set("sc_test_foo", "bar")
assert(eeprom:cfg_get("sc_test_foo") == "bar")