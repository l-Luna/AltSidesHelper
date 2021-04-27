module AltSidesHelperAltSideCassette
using ..Ahorn, Maple

@mapdef Entity "AltSidesHelper/AltSideCassette" AltSideCassette(x::Integer, y::Integer, 
   altSideToUnlock::String="", spritePath::String="", unlockText::String="", menuSprite::String="")

const placements = Ahorn.PlacementDict(
   "D-Side Cassette (AltSidesHelper)" => Ahorn.EntityPlacement(
      AltSideCassette,
      "point",
      Dict{String, Any}(
         "spritePath" => "collectables/leppa/AltSidesHelper/dside_cassette/",
         "unlockText" => "leppa_AltSidesHelper_dside_unlocked",
         "menuSprite" => "collectables/leppa/AltSidesHelper/dside_cassette"
      ),
      function(entity)
          entity.data["nodes"] = [
              (Int(entity.data["x"]) + 32, Int(entity.data["y"])),
              (Int(entity.data["x"]) + 64, Int(entity.data["y"]))
          ]
      end
   )
)

Ahorn.nodeLimits(entity::AltSideCassette) = 2, 2

sprite_suffix = "idle00.png"

function Ahorn.selection(entity::Maple.Cassette)
    x, y = Ahorn.position(entity)
    controllX, controllY = Int.(entity.data["nodes"][1])
    endX, endY = Int.(entity.data["nodes"][2])

    return [
        Ahorn.getSpriteRectangle(entity.data["spritePath"] * sprite_suffix, x, y),
        Ahorn.getSpriteRectangle(entity.data["spritePath"] * sprite_suffix, controllX, controllY),
        Ahorn.getSpriteRectangle(entity.data["spritePath"] * sprite_suffix, endX, endY)
    ]
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::Maple.Cassette)
    px, py = Ahorn.position(entity)
    nodes = entity.data["nodes"]

    for node in nodes
        nx, ny = Int.(node)

        Ahorn.drawArrow(ctx, px, py, nx, ny, Ahorn.colors.selection_selected_fc, headLength=6)
        Ahorn.drawSprite(ctx, entity.data["spritePath"] * sprite_suffix, nx, ny)
        px, py = nx, ny
    end
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::AltSideCassette, room::Maple.Room) = drawCassetteSprite(ctx, entity, 0, 0)

function drawCassetteSprite(ctx::Ahorn.Cairo.CairoContext, entity::AltSideCassette, x::Integer, y::Integer)
    Ahorn.drawSprite(ctx, entity.data["spritePath"] * sprite_suffix, x, y)
end

end