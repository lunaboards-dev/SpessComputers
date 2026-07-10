// MODULE ID: SPESSCOMPUTERS
// i'm working on it

SUBSYSTEM_DEF(spesscomputers)
    name = "SpessComputers"
    wait = 20
    dependencies = list()
    ss_flags = SS_BACKGROUND
    runlevels = RUNLEVEL_SETUP | RUNLEVEL_GAME

    var/sc_send_signal
    var/sc_tick
    var/sc_add_php
    var/list/queued_signals = list()
    var/list/queued_returns = list()
    // info about our last tick update
    var/list/last_update = list()
    // errors :(
    var/list/bwoinks = list()

/datum/controller/subsystem/spesscomputers/Initialize()
    // we're working on it
    sc_send_signal = load_ext("spesscomputers", "byond:spess_send_signal")
    sc_tick = load_ext("spesscomputers", "byond:spess_tick")
    sc_add_php = load_ext("spesscomputers", "byond:spess_register_peripheral")
    call_ext("spesscomputers", "byond:spess_init")(src)
    

/datum/controller/subsystem/spesscomputers/fire(resumed = FALSE)
    call_ext(sc_tick)(src)
    if (bwoinks.len > 0)
        for(i=1, i<=bwoinks.len, i++)
            message_admins("uncaught SpessCore error: [bwoinks[i]]")
        bwoinks.Cut()

/datum/controller/subsystem/spesscomputers/proc/SpawnComputer()

/datum/controller/subsystem/spesscomputers/proc/RegisterPeripheral(datum/spess_peripheral/peripheral)
    var/list/methods = new()
    for (var/V in peripheral.vars)
        if (findtextEx(V, "def_")==1)
            methods[copytext(V, 5)] = peripheral.vars[V]
    call_ext(sc_add_php)(methods, peripheral.type)


/datum/controller/subsystem/spesscomputers/proc/return_running()
    return last_update["running"]