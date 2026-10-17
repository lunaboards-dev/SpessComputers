/world/New()
    ..()
    var/datum/lu_blob/test_file = lu_openfile("test.bin")
    var/c = test_file.read8()
    var/r = 0
    for(var/i = 1, i <= c, i++)
        var/f = test_file.read16()
        world.log << "[r] vs [f]"
        r = test_method(r)
    var/slen = test_file.read8()
    var/date = test_file.readstr(slen)
    world.log << "date: [date]"
    var/clk = test_file.readfloat()
    var/time = test_file.readfloat(TRUE)
    world.log << "clk: [clk], time: [time]"
    shutdown()


/world/proc/test_method(x)
    x ^= 0xAA55
    x *= 3
    x += 42
    x &= 0xFFFF
    x = (x << 3) | (x >> 12)
    x &= 0xFFFF
    return x