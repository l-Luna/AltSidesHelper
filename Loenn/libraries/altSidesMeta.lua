local metaHelper = {}

-- list and group available options

metaHelper.options = {}

-- Map/OverrideVanillaSideData are handled specially
metaHelper.options.meta = {
    { "Preset", "option", { "none", "a-side", "b-side", "c-side", "d-side" } },
    { "UnlockMode", "option", { "consecutively", "always", "triggered", "with_previous", "c_sides_unlocked" } },
    { "OverrideHeartTextures", "boolean" },
    title = "meta"
}

metaHelper.options.overworld = {
    { "Icon", "string", { "menu/play", "menu/remix", "menu/rmx2", "menu/leppa/AltSidesHelper/rmx3" } },
    { "Label", "string", { "OVERWORLD_NORMAL", "OVERWORLD_REMIX", "OVERWORLD_REMIX2", "leppa_AltSidesHelper_overworld_remix3" } },
    { "DeathsIcon", "string", { "collectables/skullBlue", "collectables/skullRed", "collectables/skullGold" } },
    { "ChapterPanelHeartIcon", "string", { "collectables/heartgem/0/spin", "collectables/heartgem/1/spin", "collectables/heartgem/2/spin", "collectables/leppa/AltSidesHelper/heartgem/dside" } },
    { "JournalHeartIcon", "string", { "heartgem0", "heartgem1", "heartgem2", "leppa/AltSidesHelper/heartgemD" } },
    { "ShowBerriesAsGolden", "boolean" },
    title = "overworld"
}

metaHelper.options.inGame = {
    { "InWorldHeartIcon", "string", { "collectables/heartGem/0", "collectables/heartGem/1", "collectables/heartGem/2", "collectables/heartGem/3" } },
    { "EndScreenTitle", "string", { "AREACOMPLETE_NORMAL", "AREACOMPLETE_BSIDE", "AREACOMPLETE_CSIDE", "leppa_AltSidesHelper_areacomplete_dside" } },
    { "HeartColour", "color", { "8cc7fa", "ff668a", "fffc24", "ffffff" } },
    { "ShowHeartPoem", "boolean" },
    { "ShowBSideRemixIntro", "boolean" },
    title = "inGame"
}

metaHelper.options.fullClear = {
    { "EndScreenClearTitle", "string", { "AREACOMPLETE_NORMAL_FULLCLEAR", "leppa_AltSidesHelper_areacomplete_fullclear_bside", "leppa_AltSidesHelper_areacomplete_fullclear_cside", "leppa_AltSidesHelper_areacomplete_fullclear_dside" } },
    { "CanFullClear", "boolean" },
    { "CassetteNeededForFullClear", "boolean" },
    { "HeartNeededForFullClear", "boolean" },
    title = "fullClear"
}

metaHelper.options.experimental = {
    { "JournalCassetteIcon", "string", { "cassette", "leppa/AltSidesHelper/cassetteD" } },
    { "AddCassetteIcon", "boolean" },
    title = "experimental"
}

metaHelper.orderedOptions = {
    metaHelper.options.meta,
    metaHelper.options.overworld,
    metaHelper.options.inGame,
    metaHelper.options.fullClear,
    metaHelper.options.experimental
}

-- load options

--- load an `altsideshelper.meta.yaml` file from a full path.
---@param path string
---@return table
function metaHelper.loadMetaByPath(path)
    local yaml = require("lib.yaml.reader")
    local filesystem = require("utils.filesystem")

    if filesystem.isFile(path) then
        local f = assert(io.open(file, "rb"))
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
    local yaml = require("lib.yaml.writer")
    yaml.write(path, meta)
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