local mods = require("mods")
local utils = require("utils")
local languageRegistry = require("language_registry")

local uiElements = require("ui.elements")
local uiUtils = require("ui.utils")
local notifications = require("ui.notification")
local widgetUtils = require("ui.widgets.utils")
local lists = require("ui.widgets.lists")
local collapsable = require("ui.widgets.collapsable")
local forms = require("ui.forms.form")

local log = require("logging")

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

local function intoDefaults(tables)
    local defaults = {}

    for _, tb in ipairs(tables) do
        for _, field in ipairs(tb) do
            -- doesn't have a default -> empty string
            if field.default == nil then
                defaults[field[1]] = ""
            else
                if type(field.default) == "boolean" then
                    defaults[field[1]] = field.default
                elseif type(field.default) == "number" then
                    defaults[field[1]] = field[3][field.default]
                end
            end
        end
    end
    
    return defaults
end

--- from https://stackoverflow.com/questions/9168058/how-to-dump-a-table-to-console
---@param o table
---@return string
local function dump(o)
    if type(o) == 'table' then
        local s = '{ '
        for k,v in pairs(o) do
            if type(k) ~= 'number' then k = '"'..k..'"' end
            s = s .. '['..k..'] = ' .. dump(v) .. ','
        end
        return s .. '} '
    else
        return tostring(o)
    end
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

    local form = forms.getFormBody(values, {
        fields = intoInfos(altSidesMeta.orderedOptions),
        groups = groups,
        ignoreUnordered = true
    })

    return form
end

function metaButton.open(element)
    local language = languageRegistry.getLanguage()
    local windowTitle = tostring(language.ui.leppa.altsideshelpermeta.title)

    local values = altSidesMeta.loadMeta()
    if values and values.Sides and values.Sides[1] then
        values = values.Sides[1]
    else
        values = {}
    end
    local valuesW = intoDefaults(altSidesMeta.orderedOptions)
    for i, v in pairs(values) do
        valuesW[i] = v
    end
    
    local display = uiElements.scrollbox(uiElements.column({
        collapsable.getCollapsable("(This)", freshForm(valuesW)),
        collapsable.getCollapsable("B-Side", freshForm(valuesW))
    }))
    -- make the scrollbox Actually Work
    display:hook({
        calcWidth = function(orig, element)
            return element.inner.width
        end,
    }):with(uiUtils.fillHeight(true))

    display = uiElements.column({
        display,
        uiElements.row({
            uiElements.button("Save changes", function() end),
            uiElements.button("Add side", function() end),
            uiElements.button("Reset", function() end)
        }):with(uiUtils.bottombound)
    }):with(uiUtils.fillHeight(true))
    
    local window = uiElements.window(windowTitle, display):with({
        x = windowX,
        y = windowY,
        width = 750,
        height = 660,

        updateHidden = true
    })

    metaButton.parent:addChild(window)
    widgetUtils.addWindowCloseButton(window)

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