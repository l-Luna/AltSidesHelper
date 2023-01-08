local mods = require("mods")
local utils = require("utils")
local languageRegistry = require("language_registry")

local uiElements = require("ui.elements")
local uiUtils = require("ui.utils")
local widgetUtils = require("ui.widgets.utils")
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

--

local function centrebound(el, arg2)
    local offs
    local offsf

    local function apply(el2)
        offs, offsf = uiUtils.fract(offs, 0)
        if not offs then
            offs = 0
        end

        return uiUtils.hook(el2, {
            layoutLateLazy = function(_, self)
                -- Always reflow this child whenever its parent gets reflowed.
                self:layoutLate()
                self:repaint()
            end,

            layoutLate = function(orig, self)
                local parent = self.parent
                self.realY = math.floor(((parent.height - (parent.style:get("padding") or 0) - self.height) / 2) - offs - parent.innerHeight * offsf)
                orig(self)
            end
        })
    end

    if type(el) == "number" then
        offs = el
        return apply
    else
        offs = arg2
        return apply(el)
    end
end

--

local function save(fieldsByMap, altSideFor)
    local data = {}
    local sides = {}
    local defaults = intoDefaults(altSidesMeta.orderedOptions)

    for i, v in pairs(fieldsByMap) do
        local fieldData = forms.getFormData(v)
        local filteredData = {}

        for k2, v2 in pairs(fieldData) do
            if defaults[k2] ~= v2 then
                filteredData[k2] = v2
            end
        end

        if altSidesMeta.tableLength(filteredData) ~= 0 then
            if i == "(This)" then
                filteredData.OverrideVanillaSideData = true
            else
                filteredData.Map = i
            end

            table.insert(sides, filteredData)
        end
    end

    data.Sides = sides

    if altSideFor and altSideFor ~= "" then
        local altSideData = {}
        altSideData.IsAltSide = true
        altSideData.For = altSideFor
        data.AltSideData = altSideData
    end

    altSidesMeta.saveMeta(data)
end

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

    local form, fields = forms.getFormBody(values, {
        fields = intoInfos(altSidesMeta.orderedOptions),
        groups = groups,
        ignoreUnordered = true
    })

    return form, fields
end

function metaButton.open(_)
    local language = languageRegistry.getLanguage()
    local windowTitle = tostring(language.ui.leppa.altsideshelpermeta.title)

    local values = altSidesMeta.loadMeta()
    local defaults = intoDefaults(altSidesMeta.orderedOptions)
    local collapsableList = {}
    local fieldsByMap = {}
    local foundThis = false
    if values and values.Sides then
        for _, side in ipairs(values.Sides) do
            local sideValues = utils.deepcopy(defaults)
            for i, v in pairs(side) do
                sideValues[i] = v
            end

            local name = "<Unknown>"
            local startOpen = false
            if sideValues.OverrideVanillaSideData == true then
                name = "(This)"
                startOpen = true
                foundThis = true
            elseif sideValues.Map ~= nil then
                name = sideValues.Map
            end

            local form, fields = freshForm(sideValues)
            fieldsByMap[name] = fields
            table.insert(collapsableList, collapsable.getCollapsable(name, form, { startOpen = startOpen }))
        end
    end
    if not foundThis then
        local sideValues = utils.deepcopy(defaults)
        local name = "(This)"
        local form, fields = freshForm(sideValues)
        fieldsByMap[name] = fields
        table.insert(collapsableList, 1, collapsable.getCollapsable(name, form, { startOpen = true }))
    end
    
    local iAltSideFor = ""
    if values and values.AltSideData and values.AltSideData.IsAltSide then
        iAltSideFor = values.AltSideData.For
    end

    local display = uiElements.scrollbox(uiElements.column(collapsableList))
    -- make the scrollbox Actually Work
    display:hook({
        calcWidth = function(_, element2)
            return element2.inner.width
        end,
    }):with(uiUtils.fillHeight(true))

    local altSideToAddField = uiElements.field(""):with({ minWidth = 160, maxWidth = 160 })
    local altSideForField = uiElements.field(iAltSideFor):with({ minWidth = 160, maxWidth = 160 })

    display = uiElements.column({
        display,
        uiElements.row({
            uiElements.button("Save changes", function() save(fieldsByMap, altSideForField:getText()) end),
            uiElements.label("//"):with(centrebound),
            uiElements.button("Reset", function() end),
            uiElements.label("//"):with(centrebound),
            uiElements.button("Add side:", function() end),
            altSideToAddField,
            uiElements.label("OR mark this as alt-side for:"):with(centrebound),
            altSideForField,
        }):with(uiUtils.bottombound)
    }):with(uiUtils.fillHeight(true))

    local window = uiElements.window(windowTitle, display):with({
        x = windowX,
        y = windowY,
        width = 830,
        height = 650,

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