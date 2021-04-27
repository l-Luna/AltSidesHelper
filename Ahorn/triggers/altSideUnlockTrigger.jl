module AltSidesHelperAltSideUnlockTrigger
using ..Ahorn, Maple

@mapdef Trigger "AltSidesHelper/AltSideUnlockTrigger" AltSideUnlockTrigger(x::Integer, y::Integer, 
   width::Integer=Maple.defaultTriggerWidth, height::Integer=Maple.defaultTriggerHeight, altSideToUnlock::String="")

const placements = Ahorn.PlacementDict(
   "Alt-side Unlock Trigger (AltSidesHelper)" => Ahorn.EntityPlacement(
      AltSideUnlockTrigger,
      "rectangle",
   ),
)

end