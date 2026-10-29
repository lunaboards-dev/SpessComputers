/datum/spess_callback
    var/list/atypes = list();
    var/rtype = 0;

/datum/spess_callback/New(rettype, list/argtype)
    atypes = argtype
    rtype = rettype

/datum/spess_periph_def
    // the name of your peripheral that shows up on the computer
    var/name = ""
    // callback names and proc references
    var/list/callbacks = list();

    // set internally
    var/periph_path

/datum/spess_peripheral
    // this will be assigned by spesscore
    var/id = ""

    // (you)
    var/holder
    var/datum/spess_periph_def/def
    var/host

/datum/spess_peripheral/proc/SetID(id)

/datum/spess_peripheral/proc/Attach(computer)

/datum/spess_peripheral/proc/Detach(computer)

/datum/spess_peripheral/proc/PDestroy()

/datum/spess_peripheral/proc/RaiseEvent(name)
    