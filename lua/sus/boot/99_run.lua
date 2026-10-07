-- Start slash
local exec = require("core.exec")
local sched = require("core.sched")

local slash, env = exec.load("/bin/slash.lua")

sched.add("/bin/slash.lua", sched.current(), slash, env, {})