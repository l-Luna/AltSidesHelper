local mods = require("mods")
local utils = require("utils")
local languageRegistry = require("language_registry")

local uiElements = require("ui.elements")
local notifications = require("ui.notification")
local widgetUtils = require("ui.widgets.utils")
local forms = require("ui.forms.form")

local altSidesMeta = mods.requireFromPlugin("libraries.altSidesMeta")

--

local function intoGroups(tables)
    local groups = {}

    for _, tb in ipairs(tables) do
        local group = {}
        group["title"] = "ui.leppa.altsideshelpermeta.group." .. tb.title
        local fields = {}

        -- ipairs ignores the non-numeric `title` field
        for _, field in ipairs(tb) do
            table.insert(fields, field[1])
        end

        group["fieldOrder"] = fields
        table.insert(groups, group)
    end
    
    return groups
end

local function intoInfos(tables)
    local infos = {}

    for _, tb in ipairs(tables) do
        for _, field in ipairs(tb) do
            local info = {}

            if field[2] == "option" then
                info["fieldType"] = "string"
                info["editable"] = false
            else
                info["fieldType"] = field[2]
            end

            if #field > 2 then
                info["options"] = field[3]
            end

            infos[field[1]] = info
        end
    end

    return infos
end

--

--- tracks which alt-side has been selected for editing.
--- nil for the A-Side, or a map's name.
---@fieldType string
--[[ global ]] altSideSelected = nil

--

local metaButton = uiElements.group({})

local function freshForm(values)
    local language = languageRegistry.getLanguage()
    local groups = intoGroups(altSidesMeta.orderedOptions)
    for _, group in ipairs(groups) do
        if group.title then
            local parts = group.title:split(".")()
            local baseLanguage = utils.getPath(language, parts)
            group.title = tostring(baseLanguage.name)
        end
    end

    local form = forms.getForm({}, values, {
        fields = intoInfos(altSidesMeta.orderedOptions),
        groups = groups
    })

    return form
end

function metaButton.open(element)
    local language = languageRegistry.getLanguage()
    local windowTitle = tostring(language.ui.leppa.altsideshelpermeta.title)

    local window = uiElements.window(windowTitle, freshForm({})):with({
        x = windowX,
        y = windowY,
        width = 740,
        height = 640,

        updateHidden = true
    })

    metaButton.parent:addChild(window)
    widgetUtils.addWindowCloseButton(window)
    forms.prepareScrollableWindow(window)

    return window
end

-- thanks Just Loenny Things

local menubar = require("ui.menubar")
local mapButton = $(menubar.menubar):find(t -> t[1] == "map")

if not $(mapButton[2]):find(e -> e[1] == "leppa_altsides_meta") then
    table.insert(mapButton[2], {})
    table.insert(mapButton[2], { "leppa_altsides_meta", metaButton.open })
end

return metaButton