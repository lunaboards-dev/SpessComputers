/datum/spess
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

/datum/spess/New()

/datum/spess/proc/Init()
	set background = 1
	// we're working on it
	ws_url = world.address
	bin_name = world.system_type == "windows" ? "./spesscomputers.dll" : "./libspesscomputers.so"
	sc_tick = load_ext(bin_name, "byond:spess_tick")
	sc_create_tty = load_ext(bin_name, "byond:spess_create_tty")
	sc_pump_events = load_ext(bin_name, "byond:spess_ipc_pump")
	var/sc_register_dev = load_ext(bin_name, "byond:spess_dev_register")

	if (!call_ext(bin_name, "byond:spess_init")(src, list(
		"execpath" = "./build/spesscore",
		"workspacepath" = "build/spesscomputers",
		"ipcsocketpath" = "./sock"
	))) {
		PumpErrors()
		return SS_INIT_FAILURE
	}
	world.log  << "SpessCore started!"
	var/Deadline = world.time + (5 SECONDS)
	var/InitOK = FALSE
	sleep(1 SECONDS)
	Deadline = world.time + (5 SECONDS)
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
	var/list/devs = typesof(/datum/spess_periph_def) - /datum/spess_periph_def

	for(var/I in devs)
		var/datum/spess_periph_def/dev = new I()
		peripherals[dev.name] = dev
		call_ext(sc_register_dev)(src, dev)

	return SS_INIT_SUCCESS

/datum/spess/proc/Tick()

/datum/spess/proc/BwoinkatizeMeCaptain(bwoink)
	bwoinks += bwoink

/datum/spess/proc/PumpErrors()
	for(var/i=1, i<=bwoinks.len, i++)
		world.log << "uncaught SpessCore error: [bwoinks[i]]"
	bwoinks.Cut()