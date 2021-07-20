# AltSideHelper
AltSideHelper allows you to add extra 'sides to your map - whether they be D-Sides, Heartsides, CC-Sides, Gyms - and display them in the same way as vanilla's B and C sides. It also provides some assets for D-Sides.

## Adding alt-sides to your map
First, create your alt-sides as seperate maps. Feel free to use any naming scheme you like, so long as your don't mark it as a B-Side or C-Side (ending in `-B`, `-C`, `-H`, or `-X` is not allowed, but ending in `-D` or anything else is fine).

Then create a `mapname.altsideshelper.meta.yaml` file (analogous to Everest's `mapname.meta.yaml` file) for *both* your A-Side and alt-side. The alt-side does not need a regular `.meta.yaml` file.

The A-Side meta defines a list of additional sides to be added, and how they should appear, as well as overriding the A-Side's attributes. (Vanilla-style B-Sides and C-Sides cannot be customised in this way; use the `b-side` or `c-side` presets to create an ASH-style one instead.) The alt-side's meta tells ASH to hide it and return the player to the proper place, among other things.

## A-Side ASH meta

Here's an example for the map `examplemodder/ExampleMod/ExampleMap.bin`, with `examplemodder/ExampleMod/ExampleMap-D` to be added as a D-Side.
```yaml
Sides:
- Map: "examplemodder/ExampleMod/ExampleMap-D"
  Preset: "d-side"
```

Here's the full list of attributes that are already functional that you can set in the A-Side meta, for each side:
 - `Map`: The ID of the map to be used - the map's path from `Maps/` minus `.bin`.
 - `Preset`: One of `a-side`, `b-side`, `c-side`, and `d-side`, `none` (default), or unset. Setting this will set all attributes that you haven't specified, to follow that particular side, with `d-side` using assets provided by AltSidesHelper. You can leave this unset (or set to `none`) to set all values manually. These values are listed for each attribute.
 - `UnlockMode`: Decides when the alt-side should be available for selection. One of `consecutively` (default), which unlocks this alt-side after the previous mode; `always`, which makes this alt-side always available (if the A-Side is unlocked); `triggered`, which makes this alt-side hidden until it's unlocked by an `Alt-side Cassette` or `Alt-side Unlock Trigger`; `with_previous`, which unlocks this alt-side when the previous one is unlocked; or `c_sides_unlocked`, which unlockes this alt-side when C-Sides are unlocked for that save file.
 - `ShowBerriesAsGolden`: Decides whether strawberries should be shown as golden berries on the chapter panel, like in a vanilla B/C-Side. `false` by default, but you will want to set this for any side that has no red berries.
 - `Label`: The dialog key of text that appears when this side is selected. (A-Side is `OVERWORLD_NORMAL`, B-Side is `OVERWORLD_REMIX`, C-Side is `OVERWORLD_REMIX2`, D-Side is `leppa_AltSidesHelper_overworld_remix3`.)
 - `Icon`: The image to be displayed on the chapter select panel for that side. (`menu/play`, `menu/remix`, `menu/rmx2`, `menu/leppa/AltSidesHelper/rmx3`)
 - `DeathsIcon`: The image to be used for the deaths counter. (`collectables/skullBlue`, `collectables/skullRed`, `collectables/skullGold`, `collectables/skullGold`)
 - `ChapterPanelHeartIcon`: The sprite set to be used for the crystal heart on the chapter panel *and* when displaying the heart poem. (`collectables/heartgem/0/spin`, `collectables/heartgem/1/spin`, `collectables/heartgem/2/spin`, `collectables/leppa/AltSidesHelper/heartgem/dside` (a grey heart))
 - `HeartColour`: The colour of the text and lines in the heart poem, and the heart's particles and light. (`8cc7fa`, `ff668a`, `fffc24`, `ffffff`)
 - `InWorldHeartIcon`: The textures to be used for the crystal heart entity. (`collectables/heartGem/0/`, `collectables/heartGem/1/`, `collectables/heartGem/2/`, `collectables/heartGem/3/`)
 - `EndScreenTitle`: The dialog key of the text to be displayed on the end screen. If this is unset, or set to nothing, it won't be modified. (`AREACOMPLETE_NORMAL`, `AREACOMPLETE_BSIDE`, `AREACOMPLETE_CSIDE`, `leppa_AltSidesHelper_areacomplete_dside`)
 - `EndScreenClearTitle`: If `CanFullClear` is set to true, this dialog key will be used for the title on the end screen after a full clear. If this is unset, or set to nothing, it won't be modified. You can use this with the `b-side` or `c-side` presets to create a B/C side that can be full-cleared. (`AREACOMPLETE_NORMAL_FULLCLEAR`, `leppa_AltSidesHelper_areacomplete_fullclear_bside`, `leppa_AltSidesHelper_areacomplete_fullclear_cside`, `leppa_AltSidesHelper_areacomplete_fullclear_dside`)
 - `CanFullClear`: If true, `EndScreenClearTitle` will be used for the end screen title after a full clear. (`false`)
 - `CassetteNeededForFullClear`: Whether the player must collect a cassette (vanilla or alt-side) to full clear. (`true`)
 - `HeartNeededForFullClear`: Whether the player must collect a crystal heart to full clear. (`true`)
 - `ShowHeartPoem`: Whether the crystal heart should show text when collected. (`true` except in `c-side`.)
 - `ShowBSideRemixIntro`: Whether the music remix title, artist, and album should be displayed when entering the chapter. Setting the "`{map name}_remix_artist`", "`{map name}_remix`", and "`{map name}_remix_album`" dialog keys will display those just like a vanilla B-Side. Setting the "`{map name}_altsides_remix_intro`" dialog key will allow your to instead write your own list of text, with as many lines as you want.
 - `OverrideHeartTextures`: Whether the in-world heart, chapter panel heart, and heart poem textures and colours should be overriden to match `ChapterPanelHeartIcon`, `HeartColour`, and `InWorldHeartIcon`. `true` by default, but you might want to disable this if you're using e.g. Collab Utils 2's options for overriding the heart textures and colour.
 - `OverrideVanillaSideData`: If true, the A-Side will have its data modified, rather than creating a new side. See "Changing A-Side data". (`false`)
 - `JournalHeartIcon`: The texture to be used for the crystal heart in the journal, in the Journal atlas. Also used for the file select screen. (`heartgem0`, `heartgem1`, `heartgem2`, `leppa/AltSidesHelper/heartgemD`)

Attributes for more customisation (e.g. end screen music, columns in journal) are planned. Journal customisation will likely involve a level-set specific meta file for adding columns (such as for deaths).

## Alt-side ASH meta

Here's an example for the same map pair:
```yaml
AltSideData:
  IsAltSide: true
  For: "examplemodder/ExampleMod/ExampleMap"
```

Available attributes are:
 - `IsAltSide`: Set this to true for any alt-side. If unset, it won't act as an alt-side.
 - `For`: The ID of the A-Side map. If `IsAltSide` is set but `For` is incorrect, the alt-side will function incorrectly.
 - `CopyEndScreenData`: Whether the alt-side should use the A-Side's end screen. (True by default.)

If `CopyEndScreenData` is false, you'll need to use a seperate `mapname.meta.yaml` to set those.

## Changing A-Side data
You can modify A-Side data in exactly the same way as you would specify alt-side data. Instead of specifying a value for `Map`, set `OverrideVanillaSideData: true`. `IsAltSide` and `For` should not be set for this. Here's an example:
```yaml
Sides:
- OverrideVanillaSideData: true
  Preset: "a-side"
  CanFullClear: true
  Label: "leppa_AltSidesHelper_AltSidesHelperTest_Label"
```
This simply changes the label of the A-Side form "CLIMB" to "A-SIDE". Every preset can be used as normal.

## Alt-side Cassette and Alt-side Unlock Trigger
An alt-side that has its `UnlockMode` set to `triggered` must be unlocked using either the Alt-side Unlock Trigger or an Alt-side Cassette. The Unlock Trigger unlocks it upon being entered, and the cassette unlocks it when collected.

Both have the attribute "Alt Side To Unlock". Put in the ID of the map to be unlocked there (e.g. `examplemodder/ExampleMod/ExampleMap-D`).
The cassette has additional attributes that specify what sprites and text it uses. By default, it uses assets supplied by AltSidesHelper for a D-Side.
 - Sprite Path: The sprites to be used for the cassette in-world. `collectables/leppa/AltSidesHelper/dside_cassette/` by default. `collectables/cassette/` for the vanilla sprite.
 - Unlock Text: A comma seperated list of dialog keys for captions to be displayed in the unlock cutscene. `leppa_AltSidesHelper_dside_unlocked` ("D-Side Unlocked") by default. `OVERWORLD_REMIX_UNLOCKED` for vanilla text ("B-Side unlocked").
 - Menu Sprite: The image to be displayed in the unlock cutscene. `collectables/leppa/AltSidesHelper/dside_cassette` by default. `collectables/cassette` for the vanilla sprite.

## Verbose Logging

To enable verbose logging, open `modsettings-Everest.celeste` in your saves folder, and replace the line beginning with `LogLevels` with:

```yaml
LogLevels:
  '': Info
  AltSidesHelper: Verbose
```

This causes most customisations to be logged too. You probably don't want this, unless you're a mod author investigating an incompatibility, or you're sending me a bug report.