var/datum/spess/s_core

/world

/world/New()
    ..()
    s_core = new()
    s_core.Init()

/world/proc/Run()
    set waitfor = 0
    RunTick()

/world/proc/RunTick()
    s_core.Tick()

    spawn(1)
        RunTick()
