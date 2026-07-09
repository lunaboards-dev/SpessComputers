local net = {}

--[[
struct net_pkt {
    u16 real_port;
    u16 flags;
    u16 mtu;
    u8 count;
    u8 max;
    u16 content_len;
}
]]

function net.send(dst, port, data, opt)
    local euid, egid = os.geteid()
    if port <= 1024 and euid ~= 0 and egid ~= 0 then
        
    end
    if opt.secure then

    end
end

function net.init()

end