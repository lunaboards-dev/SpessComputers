local BOOT = ...
BOOT.status(string.format("mounting %s on /", BOOT.root.dev.id))
BOOT.vfs = BOOT.loadfile("/lib/core/vfs.lua")()
BOOT.vfs.mount("/", BOOT.root)