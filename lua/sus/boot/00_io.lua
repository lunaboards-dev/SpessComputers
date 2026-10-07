local boot = ...
local fs = boot.loadfile("/lib/core/fs.lua")()
boot.fs = fs

fs.mount("/", boot.root)