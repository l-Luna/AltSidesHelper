# AltSideHelper
AltSideHelper allows you to add extra 'sides to your map - whether they be D-Sides, Heartsides, CC-Sides, Gyms - and display them in the same way as vanilla's B and C sides. It also provides some assets for D-Sides.

## Adding alt-sides to your map
First, create your alt-sides as seperate maps. Feel free to use any naming scheme you like, so long as your don't mark it as a B-Side or C-Side (ending in `-B`, `-C`, `-H`, or `-X` - `-D` is fine).

Then create a `mapname.altsideshelper.meta.yaml` file (analogous to Everest's `mapname.meta.yaml` file) for *both* your A-Side and alt-side. The alt-side does not need a regular `.meta.yaml` file.

The A-Side meta defines a list of additional sides to be added, and how they should appear. (This will eventually be extended to allow overriding vanilla side attributes.) The alt-side's meta tells ASH to hide it and return the player to the proper place, among other things.

## A-Side ASH meta

Here's an example for the map `leppa/RecursiveSpace/RecursiveSpace.bin`, with `leppa/RecursiveSpace/RecursiveSpace-D` to be added as a D-Side.
```yaml
Sides:
- Map: "leppa/RecursiveSpace/RecursiveSpace-D"
  Preset: "d-side"
```

Here's the full list of attributes that are already functional that you can set in the A-Side meta, for each side:
 - `Map`: The ID of the map to be used - the map's path from `Maps/` minus `.bin`.
 - `Preset`: One of `a-side`, `b-side`, `c-side`, and `d-side`. Setting this overrides all attributes minus `Map` to follow that particular side, with `d-side` using assets provided by AltSidesHelper. These values are listed for each attribute.
 - `Label`: The dialog key of text that appears when this side is selected. (A-Side is `OVERWORLD_NORMAL`, B-Side is `OVERWORLD_REMIX`, C-Side is `OVERWORLD_REMIX2`, D-Side is `leppa_AltSidesHelper_overworld_remix3`.)
 - `Icon`: The image to be displayed on the chapter select panel for that side. (`menu/play`, `menu/remix`, `menu/rmx2`, `menu/leppa/AltSidesHelper/rmx3`)
 - `DeathsIcon`: The image to be used for the deaths counter. (`collectables/skullBlue`, `collectables/skullRed`, `collectables/skullGold`, `collectables/skullGold`)
 - `ChapterPanelHeartIcon`: The sprite set to be used for the crystal heart on the chapter panel *and* when displaying the heart poem. (`collectables/heartgem/0/spin`, `collectables/heartgem/1/spin`, `collectables/heartgem/2/spin`, `collectables/leppa/AltSidesHelper/heartgem/dside` (a grey heart))
 - `PoemDisplayColor`: The colour of the text and lines in the heart poem. (`8cc7fa`, `ff668a`, `fffc24`, `ffffff`)

The following attributes can be set, but are currently unimplemented:
 - `InWorldHeartIcon`: The textures to be used for the crystal heart entity. (`collectables/heartGem/0`, `collectables/heartGem/1`, `collectables/heartGem/2`, `collectables/heartGem/3`)
 - `JournalHeartIcon`: The texture to be used for the crystal heart in the journal. (`heartgem0`, `heartgem1`, `heartgem2`, `heartgem0`)
 - `OverrideVanillaSideData`: If true, the vanilla sid chosen by `VanillaSide` will have its data modified, rather than creating a new side. (`false`.)
 - `VanillaSide`: If `OverrideVanillaSideData` is true, the side chosen by this will have its data overriden. (Empty.)

Attributes for unlocking alt-sides (by cassette or consecutively) and more journal information are planned. Journal customisation will likely involve a level-set specific meta file for adding columns (such as for deaths).

## Alt-side ASH meta

Here's an example for the same map pair:
```yaml
AltSideData:
  IsAltSide: true
  For: "leppa/RecursiveSpace/RecursiveSpace"
```

Available attributes are:
 - `IsAltSide`: Set this to true for any alt-side. If unset, it won't act as an alt-side.
 - `For`: The ID of the A-Side map. If `IsAltSide` is set but `For` is incorrect, the alt-side will function incorrectly.
 - `CopyMountainData`: Whether the alt-side should use the A-Side's mountain location... and is currently broken. (True by default.)
 - `CopyEndScreenData`: Whether the alt-side should use the A-Side's end screen. (True by default.)

If `CopyEndScreenData` or `CopyMountainData` are false, you'll need to use a seperate `mapname.meta.yaml` to set those. Mountain location is currently bugged. An attribute to set the alt-side's end screen title seperately is planned.
