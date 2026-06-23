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
    var/list/queued_signals = list()
    var/list/queued_returns = list()
    // info about our last tick update
    var/list/last_update = list()
    // errors :(
    var/list/bwoinks = list()
    // if you varedit this i will kill you
    var/socket_fd

/datum/controller/subsystem/spesscomputers/Initialize()
    // we're working on it
    sc_send_signal = load_ext("spesscomputers", "byond:spess_send_signal")
    sc_tick = load_ext("spesscomputers", "byond:spess_tick")
    call_ext("spesscomputers", "byond:spess_init")(src)
    

/datum/controller/subsystem/spesscomputers/fire(resumed = FALSE)
    call_ext(sc_tick)(src)
    if (bwoinks.len > 0)
        for(i=1, i<=bwoinks.len, i++)
            message_admins("uncaught error: [bwoinks[i]]")
        bwoinks.Cut()

/datum/controller/subsystem/spesscomputers/proc/SpawnComputer()

/datum/controller/subsystem/spesscomputers/proc/RegisterPeripheral()

/datum/controller/subsystem/spesscomputers/proc/return_running()
    return last_update["running"]