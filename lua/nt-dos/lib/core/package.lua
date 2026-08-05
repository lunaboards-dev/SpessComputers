local preload, loadas = ...

local function create_package_instance(euid, egid)
    local package = {}
    package.loaders = {}
    package.loaded = {}
    package.path = "./?.lua;./?/init.lua;/lib/?.lua;/lib/?/init.lua"
    function package.require(pkg)
        if package.loaded[pkg] then return package.loaded[pkg] end
        local lines = {}
        for i=1, #package.loaders do
            local func, err = package.loaders[i](pkg)
            if func then
                local ok, res = pcall(func)
                if not ok then
                    return nil, res
                end
                package.loaded[pkg] = res
                return res
            else
                table.insert(lines, err)
            end
        end
        return nil, table.concat(lines)
    end

    local function load_preload(pkg)
        if preload[pkg] then
            return function()return preload[pkg]end
        end
        return nil, "no kernel preload[\""..pkg.."\"]"
    end

    local function load_path(pkg)

    end

    return package
end

return create_package_instance