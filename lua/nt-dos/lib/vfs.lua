local vfs = {}

local hand = {}

local function create_hand(dev, dir, name)
    local inode = dev.select("FileMetadata", {
        Store = dir,
        Name = name
    })
    dev.query("handle", "SELECT * FROM FileData WHERE Inode=@inode SORT ASC")
end