local mods = require("mods")

local utils = require("utils")

local uiElements = require("ui.elements")
local widgetUtils = require("ui.widgets.utils")
local form = require("ui.forms.form")
local languageRegistry = require("language_registry")
local notifications = require("ui.notification")

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

local metaButton = uiElements.group({})

metaButton.open = function (element)
    local language = languageRegistry.getLanguage()
    
    local windowTitle = tostring(language.ui.leppa.altsideshelpermeta.title)

    groups = utils.deepcopy(intoGroups(altSidesMeta.orderedOptions))
    for _, group in ipairs(groups) do
        if group.title then
            local parts = group.title:split(".")()
            local baseLanguage = utils.getPath(language, parts)
            group.title = tostring(baseLanguage.name)
        end
    end
    
    local metadataForm = form.getForm({}, {}, {
        fields = intoInfos(altSidesMeta.orderedOptions),
        groups = groups
    })

    local window = uiElements.window(windowTitle, metadataForm):with({
        x = windowX,
        y = windowY,

        updateHidden = true
    })

    metaButton.parent:addChild(window)
    widgetUtils.addWindowCloseButton(window)
    form.prepareScrollableWindow(window)

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