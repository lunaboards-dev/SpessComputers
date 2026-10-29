// MODULE ID: SPESSCOMPUTERS
// i'm working on it

#define SC_CON_OK 0
#define SC_CON_RETRY 1
#define SC_CON_DEAD 2

SUBSYSTEM_DEF(spesscomputers)
	name = "SpessComputers"
	wait = 20
	dependencies = list()
	ss_flags = SS_BACKGROUND
	runlevels = RUNLEVEL_SETUP | RUNLEVEL_GAME

	var/sc_send_signal
	var/sc_tick
	var/sc_add_php
	var/sc_create_tty
	var/sc_pump_events
	var/list/queued_signals = list()
	var/list/queued_returns = list()
	// info about our last tick update
	var/list/last_update = list()

    var/list/peripherals = list()
	// errors :(
	var/list/bwoinks = list()
	var/list/proc_is_kill = FALSE
	var/list/proc_is_crash = FALSE
	var/list/proc_error
	var/bin_name
	var/ws_url
	var/ws_port = 42069

/datum/controller/subsystem/spesscomputers/Initialize()
	// we're working on it
	ws_url = world.address
	bin_name = world.system_type == "windows" ? "./spesscomputers.dll" : "./libspesscomputers.so"
	sc_tick = load_ext(bin_name, "byond:spess_tick")
	sc_create_tty = load_ext(bin_name, "byond:spess_create_tty")
	sc_pump_events = load_ext(bin_name, "byond:spess_ipc_pump")
    var/sc_register_dev = load_ext(bin_name, "byond:spess_register_dev")

	if (!call_ext(bin_name, "byond:spess_init")(src, list(
		"execpath" = "./spesscore/spesscore",
		"workspacepath" = "spesscomputers",
		"ipcsocketpath" = "./sock"
	))) {
		PumpErrors()
		return SS_INIT_FAILURE
	}
	notice("SpessCore started!")
	var/Deadline = world.time + (5 SECONDS)
    var/InitOK = FALSE

	while (world.time < Deadline)
		var/stat = call_ext(bin_name, "byond:spess_init_try_connect")(src)
		PumpErrors()
		if (stat == 0)
			//return SS_INIT_SUCCESS
            InitOK = TRUE
            break
		else if (stat == 2)
			return SS_INIT_FAILURE

    if (!InitOK)
        return SS_INIT_FAILURE

    // We have liftoff! Time to register all of our devices...
    var/list/devs = subtypesof(/datum/spess_periph_def)

    for(var/I in devs)
        var/dev = new I()
        peripherals[dev.name] = dev
        call_ext(sc_register_dev)(src, dev)

	return SS_INIT_SUCCESS


/datum/controller/subsystem/spesscomputers/fire(resumed = FALSE)
	call_ext(sc_tick)(src)
	PumpErrors()
	while (call_ext(sc_pump_events)(src))
		PumpErrors()

/datum/controller/subsystem/spesscomputers/proc/BwoinkatizeMeCaptain(bwoink)
	bwoinks += bwoink

/datum/controller/subsystem/spesscomputers/proc/PumpErrors()
	for(var/i=1, i<=bwoinks.len, i++)
		warning("uncaught SpessCore error: [bwoinks[i]]")
	bwoinks.Cut()

/datum/controller/subsystem/spesscomputers/proc/SpawnComputer()

/* /datum/controller/subsystem/spesscomputers/proc/RegisterPeripheral(datum/spess_peripheral/peripheral)
	var/list/methods = new()
	for (var/V in peripheral.vars)
		if (findtextEx(V, "def_")==1)
			methods[copytext(V, 5)] = peripheral.vars[V]
	call_ext(sc_add_php)(methods, peripheral.type)
 */

/datum/controller/subsystem/spesscomputers/proc/CreateTTY(obj/holder)
	var/datum/sc_holder/tty/ttyobj = new(holder)
	call_ext(sc_create_tty)(src, ttyobj)
	return ttyobj

/datum/controller/subsystem/spesscomputers/proc/CreateBIOS(obj/item/sc_component/bios/eeprom)
	// call_ext(sc_create_bios)(src, eeprom.holder, eeprom.read_only, eeprom.data_path)

/datum/controller/subsystem/spesscomputers/proc/return_running()
	return last_update["running"]

#undef SC_CON_OK
#undef SC_CON_RETRY
#undef SC_CON_DEAD
