local cassette = {}

cassette.name = "AltSidesHelper/AltSideCassette"
cassette.depth = -1000000
cassette.nodeLineRenderType = "line"
cassette.texture = "collectables/cassette/idle00"
cassette.nodeLimits = {2, 2}
cassette.placements = {
    name = "default",
    data = {
        altSideToUnlock = "",
        spritePath = "collectables/leppa/AltSidesHelper/dside_cassette/",
        unlockText = "leppa_AltSidesHelper_dside_unlocked",
        menuSprite = "collectables/leppa/AltSidesHelper/dside_cassette"
    }
}

function cassette.texture(room, entity)
    return entity.spritePath .. "idle00"
end

return cassette