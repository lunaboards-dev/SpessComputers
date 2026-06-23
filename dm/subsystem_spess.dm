// MODULE ID: SPESSCOMPUTERS
// i'm working on it

SUBSYSTEM_DEF(spesscomputers)
    name = "SpessComputers"
    wait = 0
    dependencies = list()
    ss_flags = SS_BACKGROUND
    runlevels = RUNLEVEL_SETUP | RUNLEVEL_GAME

    var/sc_init
    var/sc_send_signal

/datum/controller/subsystem/spesscomputers/Initialize()
    // we're working on it

/datum/controller/subsystem/spesscomputers/fire(resumed = FALSE)
    // yeah

/datum/controller/subsystem/spesscomputers/proc/SpawnComputer()

/datum/controller/subsystem/spesscomputers/proc/RegisterPeripheral()
