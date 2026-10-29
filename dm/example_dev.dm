SPESS_DEVICE(example)
    name = "example"
    callbacks = list(
        SPESS_METHOD(/datum/spess_peripheral/example/proc/test, SC_TBOOL, list(SC_TFLOAT, SC_TFLOAT, SC_TFLOAT | SC_TNULLABLE))
        SPESS_METHOD(/datum/spess_peripheral/example/proc/log, SC_NONE, list(SC_TANY))
    )
    
SPESS_DEVICE(example)/proc/test(computer, a, b, c)
    return a+b == c

SPESS_DEVICE(example)/proc/log(computer, msg)
    world.log << "(computer [computer]): [msg]"