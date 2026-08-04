local M = {}

local emt = {}

function emt:__tostring()
    return (self[1]:gsub("@",""))
end

function emt:__call(...)
    local i = 0
    local r = table.pack(...)
    return (self[1]:gsub("@", function(m)
        i = i + 1
        return tostring(r[i] or "")
    end))
end

local function p_esc(str)
    return setmetatable({"\27["..str}, emt)
end

local function b_esc(str)
    return "\27["..str
end

-- basic stuff

--- Device status report.
--- Writes cursor position to stdin as `ESC[y;xR`.
M.dsr = b_esc("6n")
--- Aux port on
M.aux_on = b_esc("5i")
--- Aux port off
M.aux_off = b_esc("4i")
--- Save cursor pos
M.scp = b_esc("s")
--- Restore cursor pos
M.rcp = b_esc("u")
--- Show cursor
M.dec_sc = b_esc("?25h")
--- Hide cursor
M.dec_hc = b_esc("?25l")

--- Cursor up
M.cuu = p_esc("@A")
--- Cursor down
M.cud = p_esc("@B")
--- Cursor right
M.cuf = p_esc("@C")
--- Cursor left
M.cub = p_esc("@D")
--- Cursor next line
M.cnl = p_esc("@E")
--- Cursor previous line
M.cpl = p_esc("@F")
--- Cursor horizontal absolute position
M.cha = p_esc("@G")
--- Cursor absolute position
M.cup = p_esc("@;@H")
--- Erase display
M.ed = p_esc("@J")
--- Erase line
M.el = p_esc("@K")
--- Scroll screen up
M.su = p_esc("@S")
--- Scroll screen down
M.sd = p_esc("@T")
--- Horizontal-Vertical position.
--- *May function differently from CUP!*
M.hvp = p_esc("@;@f")
--- Select graphic rendition.
--- Allows styling.
M.sgi = p_esc("@m")

-- FORMATTING

--- Reset all styling
M.sgi.reset = M.sgi(0)
--- Increased intensity. May change color.
M.sgi.iint = M.sgi(1)
--- Bold. Alias for `iint`.
M.sgi.bold = M.sgi.iint
--- Decreased intensity. May change color.
M.sgi.dint = M.sgi(2)
--- Italic. Not well supported.
--- Might show up as inverted colors or blinking.
M.sgi.ital = M.sgi(3)
--- Underline. Not well supported.
M.sgi.ul = M.sgi(4)
--- Slow blink.
M.sgi.blnk = M.sgi(5)
--- Rapid blink. Not well supported.
M.sgi.rblnk = M.sgi(6)
--- Swaps foreground-background color.
M.sgi.ivrt = M.sgi(7)
--- Hides text.
M.sgi.hide = M.sgi(8)
--- Strikes through text.
M.sgi.strk = M.sgi(9)
--- Primary font.
M.sgi.pfnt = M.sgi(10)
--- Alternate fonts.
M.sgi.afnt = {}
for i=1, 9 do
    M.sgi.afnt[i] = M.sgi(10+i)
end
--- Franktur/Gothic font. Not well supported.
M.sgi.goth = M.sgi(20)
--- Double underline. Not well supported.
M.sgi.dul = M.sgi(21)
--- Neither bold nor faint.
M.sgi.nint = M.sgi(22)
--- Neither italic nor blackletter.
M.sgi.ninb = M.sgi(23)
--- Not underlined.
M.sgi.nul = M.sgi(24)
--- Not blinking.
M.sgi.nblnk = M.sgi(25)
--- Proportional spacing. Not well supported.
M.sgi.ppsp = M.sgi(26)
--- Not inverted.
M.sgi.ninv = M.sgi(27)
--- Reveal text.
M.sgi.revl = M.sgi(28)
--- Not crosed out.
M.sgi.ncrs = M.sgi(29)
--- Reset foreground.
M.sgi.rfg = M.sgi(39)
--- Reset background.
M.sgi.rbg = M.sgi(49)
--- Disable proportional space.
M.sgi.dpsp = M.sgi(50)

-- Colors

--- Black, foreground
M.sgi.fgbl = M.sgi(30)
--- Black, background
M.sgi.bgbl = M.sgi(40)

--- Red, foreground
M.sgi.fgdr = M.sgi(31)
M.sgi.bgdr = M.sgi(41)

M.sgi.fgdg = M.sgi(32)
M.sgi.bgdg = M.sgi(42)

M.sgi.fgdy = M.sgi(33)
M.sgi.bgdy = M.sgi(43)

M.sgi.fgdb = M.sgi(34)
M.sgi.bgdb = M.sgi(44)

M.sgi.fgdm = M.sgi(35)
M.sgi.bgdm = M.sgi(45)

M.sgi.fgdc = M.sgi(36)
M.sgi.bgdc = M.sgi(46)

M.sgi.fgdw = M.sgi(37)
M.sgi.bgdw = M.sgi(47)

M.sgi.fggy = M.sgi(90)
M.sgi.bggy = M.sgi(100)

return M