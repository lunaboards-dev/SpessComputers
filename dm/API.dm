/datum/spess_peripheral
    // name of your peripheral
    var/name = ""

    // this will be assigned by spesscore
    var/id = ""

    // callback names and proc references
    var/list/callbacks = list();

    // (you)
    var/holder

/datum/spess_peripheral/proc/Attach(computer)

/datum/spess_peripheral/proc/Detach(computer)

/datum/spess_peripheral/proc/PDestroy()