local metaHelper = {}

-- list and group available options

-- each option has the following structure:
-- [1]: string: name (e.g. Label, InWorldHeartIcon)
-- [2]: string: type (e.g. string, boolean, option)
-- [3]: optional list of strings: suggested options
-- [default]: optional default value of that field, either:
    -- a number, referring back to an entry in [3] by index
    -- a boolean, for a literal boolean value
-- preset based default values are handled with empty strings and not mentioned

metaHelper.options = {}

-- Map/OverrideVanillaSideData are handled specially
metaHelper.options.meta = {
    { "Preset", "option", { "none", "a-side", "b-side", "c-side", "d-side" }, default = 1 },
    { "UnlockMode", "option", { "consecutively", "always", "triggered", "with_previous", "c_sides_unlocked" }, default = 1 },
    { "OverrideHeartTextures", "boolean", default = true },
    title = "meta"
}

metaHelper.options.overworld = {
    { "Icon", "string", { "menu/play", "menu/remix", "menu/rmx2", "menu/leppa/AltSidesHelper/rmx3" } },
    { "Label", "string", { "OVERWORLD_NORMAL", "OVERWORLD_REMIX", "OVERWORLD_REMIX2", "leppa_AltSidesHelper_overworld_remix3" } },
    { "DeathsIcon", "string", { "collectables/skullBlue", "collectables/skullRed", "collectables/skullGold" } },
    { "ChapterPanelHeartIcon", "string", { "collectables/heartgem/0/spin", "collectables/heartgem/1/spin", "collectables/heartgem/2/spin", "collectables/leppa/AltSidesHelper/heartgem/dside" } },
    { "JournalHeartIcon", "string", { "heartgem0", "heartgem1", "heartgem2", "leppa/AltSidesHelper/heartgemD" } },
    { "ShowBerriesAsGolden", "boolean", default = false },
    title = "overworld"
}

metaHelper.options.inGame = {
    { "InWorldHeartIcon", "string", { "collectables/heartGem/0", "collectables/heartGem/1", "collectables/heartGem/2", "collectables/heartGem/3" } },
    { "EndScreenTitle", "string", { "AREACOMPLETE_NORMAL", "AREACOMPLETE_BSIDE", "AREACOMPLETE_CSIDE", "leppa_AltSidesHelper_areacomplete_dside" } },
    { "HeartColour", "color", { "8cc7fa", "ff668a", "fffc24", "ffffff" } },
    { "ShowHeartPoem", "boolean", default = true }, -- TODO: except in c-sides!!
    { "ShowBSideRemixIntro", "boolean", default = false },
    title = "inGame"
}

metaHelper.options.fullClear = {
    { "EndScreenClearTitle", "string", { "AREACOMPLETE_NORMAL_FULLCLEAR", "leppa_AltSidesHelper_areacomplete_fullclear_bside", "leppa_AltSidesHelper_areacomplete_fullclear_cside", "leppa_AltSidesHelper_areacomplete_fullclear_dside" } },
    { "CanFullClear", "boolean", default = false },
    { "CassetteNeededForFullClear", "boolean", default = true },
    { "HeartNeededForFullClear", "boolean", default = true },
    title = "fullClear"
}

metaHelper.options.experimental = {
    { "JournalCassetteIcon", "string", { "cassette", "leppa/AltSidesHelper/cassetteD" } },
    { "AddCassetteIcon", "boolean", default = false },
    title = "experimental"
}

metaHelper.orderedOptions = {
    metaHelper.options.meta,
    metaHelper.options.overworld,
    metaHelper.options.inGame,
    metaHelper.options.fullClear,
    metaHelper.options.experimental
}

-- util stuff

---@param table table
---@return number
function metaHelper.tableLength(table)
    local count = 0
    for _ in pairs(table) do count = count + 1 end
    return count
end


---@param o table
---@return string
function metaHelper.intoYaml(o, ident, blockhint)
    ident = ident or 0
    blockhint = blockhint or false
    if type(o) == 'table' then
        -- is it a list or a object?
        if metaHelper.tableLength(o) == 0 then
            return "{}"
        end
        if o[1] ~= nil then
            local s = ""
            for _, v in ipairs(o) do
                s = s .. "\n" .. string.rep(" ", ident) .. "- " .. metaHelper.intoYaml(v, ident + 2, true)
            end
            return s
        end
        local s = ""
        for k, v in pairs(o) do
            s = s .. "\n" .. string.rep(" ", ident) .. tostring(k) .. ": " .. metaHelper.intoYaml(v, ident + 2)
        end
        if blockhint then
            s = string.sub(s, 2 + ident)
        end
        return s
    elseif type(o) == "string" then
        return "\"" .. o .. "\""
    else
        return tostring(o)
    end
end

-- load options

--- load an `altsideshelper.meta.yaml` file from a full path.
---@param path string
---@return table
function metaHelper.loadMetaByPath(path)
    local yaml = require("lib.yaml.reader")
    local filesystem = require("utils.filesystem")

    if filesystem.isFile(path) then
        local f = assert(io.open(path, "r"))
        local content = f:read("*all")
        f:close()
        
        return yaml.eval(content)
    else
        return {}
    end
end

--- load the current map's `altsideshelper.meta.yaml` file, if it exists.
--- returns a table containing loaded options (empty if it doesn't exist), or nil if the current map is not saved.
---@return table
function metaHelper.loadMeta()
    local loadedState = require("loaded_state")
    local baseFile = loadedState.filename
    if not baseFile then
        return nil
    end
    
    local metaFile = string.sub(baseFile, 1, -5) .. ".altsideshelper.meta.yaml"
    return metaHelper.loadMetaByPath(metaFile)
end

--- saves the given metadata to the given path.
---@param meta table
---@param path string
---@return void
function metaHelper.saveMetaToPath(meta, path)
    local filesystem = require("utils.filesystem")
    
    if filesystem.isFile(path) then
        ---@type file
        local f = assert(io.open(path, "w"))
        f:write(metaHelper.intoYaml(meta))
        f:close()
    end
end

--- saves the given metadata to the appropriate place for this map; does nothing if the map is not saved.
---@param meta table
---@return void
function metaHelper.saveMeta(meta)
    local loadedState = require("loaded_state")
    local baseFile = loadedState.filename
    if not baseFile then
        return
    end

    local metaFile = string.sub(baseFile, 1, -5) .. ".altsideshelper.meta.yaml"
    metaHelper.saveMetaToPath(meta, metaFile)
end

return metaHelper