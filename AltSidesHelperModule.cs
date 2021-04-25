using Celeste.Mod;
using Celeste;
using System.Reflection;
using System;
using System.Collections;
using MonoMod.Utils;
using MonoMod.RuntimeDetour;
using System.Collections.Generic;
using Monocle;
using System.Xml;
using Microsoft.Xna.Framework;

namespace AltSidesHelper {
	public class AltSidesHelperModule : EverestModule {

		public static AltSidesHelperModule Instance;

		// hooks
		private static IDetour hook_OuiChapterPanel_set_option;
		private static IDetour hook_OuiChapterPanel_get_option;
		private static IDetour hook_OuiChapterSelect_get_area;

		// variables used for returning from alt-sides to the chapter panel
		private int returningAltSide = -1;
		private bool shouldResetStats = true;

		// types and fields invoked via reflection
		private static readonly Type t_OuiChapterPanelOption = typeof(OuiChapterPanel)
			.GetNestedType("Option", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		private static FieldInfo modesField = typeof(OuiChapterPanel)
			.GetField("modes", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
		private static MethodInfo resetMethod = typeof(OuiChapterPanel)
			.GetMethod("Reset", BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.Instance);

		// heart display in chapter panel
		public static SpriteBank HeartSpriteBank;
		public static SpriteBank WorldHeartSpriteBank;

		public AltSidesHelperModule() {
			Instance = this;
		}

		public override void Load() {
			On.Celeste.OuiChapterPanel.Reset += CustomiseChapterPanel;
			On.Celeste.OuiChapterPanel.IsStart += FixReturnFromAltSide;
			On.Celeste.OuiChapterPanel.UpdateStats += FixSettingAltSideStats;
			On.Celeste.OuiChapterPanel.SetStatsPosition += FixSettingAltSideStatPositions;
			On.Celeste.AreaData.Load += PostAreaLoad;
			On.Celeste.OuiChapterSelect.Added += HideAltSides;
			On.Celeste.OuiChapterSelect.IsStart += ReturnToAltSide;
			On.Celeste.Poem.ctor += SetPoemColour;
			On.Celeste.DeathsCounter.SetMode += SetDeathsCounterIcon;
			On.Celeste.HeartGem.Awake += SetCrystalHeartSprite;

			//IL.Celeste.OuiJournalProgress.ctor += OnJournalProgressPageConstruct;

			hook_OuiChapterPanel_set_option = new Hook(
				typeof(OuiChapterPanel).GetProperty("option", BindingFlags.NonPublic | BindingFlags.Instance).GetSetMethod(true),
				typeof(AltSidesHelperModule).GetMethod("OnChapterPanelChangeOption", BindingFlags.NonPublic | BindingFlags.Static)
			);
			hook_OuiChapterPanel_get_option = new Hook(
				typeof(OuiChapterPanel).GetProperty("option", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true),
				typeof(AltSidesHelperModule).GetMethod("OnChapterPanelGetOption", BindingFlags.NonPublic | BindingFlags.Static)
			);
			hook_OuiChapterSelect_get_area = new Hook(
				typeof(OuiChapterSelect).GetProperty("area", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true),
				typeof(AltSidesHelperModule).GetMethod("OnChapterSelectGetArea", BindingFlags.NonPublic | BindingFlags.Static)
			);
		}

		public override void LoadContent(bool firstLoad) {
			
		}

		public override void Unload() {
			On.Celeste.OuiChapterPanel.Reset -= CustomiseChapterPanel;
			On.Celeste.OuiChapterPanel.IsStart -= FixReturnFromAltSide;
			On.Celeste.OuiChapterPanel.UpdateStats -= FixSettingAltSideStats;
			On.Celeste.OuiChapterPanel.SetStatsPosition -= FixSettingAltSideStatPositions;
			On.Celeste.AreaData.Load -= PostAreaLoad;
			On.Celeste.OuiChapterSelect.Added -= HideAltSides;
			On.Celeste.OuiChapterSelect.IsStart -= ReturnToAltSide;
			On.Celeste.Poem.ctor -= SetPoemColour;
			On.Celeste.DeathsCounter.SetMode -= SetDeathsCounterIcon;
			On.Celeste.HeartGem.Awake -= SetCrystalHeartSprite;

			//IL.Celeste.OuiJournalProgress.ctor -= OnJournalProgressPageConstruct;

			hook_OuiChapterPanel_set_option.Dispose();
			hook_OuiChapterPanel_get_option.Dispose();
			hook_OuiChapterSelect_get_area.Dispose();
		}

		private void SetCrystalHeartSprite(On.Celeste.HeartGem.orig_Awake orig, HeartGem self, Scene scene) {
			orig(self, scene);
			if(!self.IsGhost && !self.IsFake) {
				var meta = GetModeMetaForAltSide(AreaData.Get((scene as Level).Session.Area));
				if(meta != null) {
					var selfdata = new DynData<HeartGem>(self);
					var sprite = new Sprite(GFX.Game, meta.InWorldHeartIcon);
					sprite.CenterOrigin();
					sprite.AddLoop("idle", "", 0, new int[] { 0 });
					sprite.AddLoop("spin", "", 0.1f, new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 });
					sprite.AddLoop("fastspin", "", 0.1f);
					sprite.CenterOrigin();
					sprite.OnLoop = delegate (string anim) {
						if(self.Visible && anim == "spin" && (bool)selfdata["autoPulse"]) {
							Audio.Play("event:/game/general/crystalheart_pulse", self.Position);
							self.ScaleWiggler.Start();
							(scene as Level).Displacement.AddBurst(self.Position, 0.35f, 8f, 48f, 0.25f);
						}
					};
					sprite.Play("spin");
					self.ScaleWiggler.RemoveSelf();
					self.ScaleWiggler = Wiggler.Create(0.5f, 4f, delegate (float f) {
						sprite.Scale = Vector2.One * (1f + f * 0.25f);
					});
					self.Add(self.ScaleWiggler);
					((Component)selfdata["sprite"]).RemoveSelf();
					selfdata["sprite"] = sprite;
					self.Add(sprite);
				}
			}
		}

		private void SetDeathsCounterIcon(On.Celeste.DeathsCounter.orig_SetMode orig, DeathsCounter self, AreaMode mode) {
			orig(self, mode);
			if(self.Entity is OuiChapterPanel panel) {
				var meta = GetModeMetaForAltSide(panel.Data);
				if(meta != null)
					new DynData<DeathsCounter>(self).Set("icon", GFX.Gui[meta.DeathsIcon]);
			}
		}

		private void CustomiseChapterPanel(On.Celeste.OuiChapterPanel.orig_Reset orig, OuiChapterPanel self) {
			ResetCrystalHeart(self);
			
			var oldRealStats = self.RealStats;
			var oldDisplayedStats = self.DisplayedStats;
			
			orig(self);
			AddExtraModes(self);

			// check if we're returning from an alt-side
			var selfdata = new DynData<OuiChapterPanel>(self);
			if(returningAltSide < 0) {
				selfdata.Data.Remove("TrueMode");
			} else {
				selfdata["TrueMode"] = returningAltSide;
				//Logger.Log("AltSidesHelper", $"returningAltSide = {returningAltSide}, mode count = {((IList)modesField.GetValue(self)).Count}.");
				// only run this when called in the correct
				if(!shouldResetStats)
					UpdateDataForTrueMode(self, returningAltSide);
			}

			if(!shouldResetStats) {
				self.RealStats = oldRealStats;
				self.DisplayedStats = oldDisplayedStats;
			}
			CustomizeCrystalHeart(self);
		}

		// Copied from CollabUtils2: https://github.com/EverestAPI/CelesteCollabUtils2/blob/7b9cfbfa6551c68aad98273de4b7ba00dd29e22d/UI/InGameOverworldHelper.cs
		// As = AltSides
		private static void ResetCrystalHeart(OuiChapterPanel panel) {
			DynData<OuiChapterPanel> panelData = new DynData<OuiChapterPanel>(panel);
			if(panelData.Data.ContainsKey("AsHeartDirty") && panelData.Get<bool>("AsHeartDirty")) {
				panel.Remove(panelData["heart"] as HeartGemDisplay);
				panelData["heart"] = new HeartGemDisplay(0, false);
				panel.Add(panelData["heart"] as HeartGemDisplay);
				panelData["AsHeartDirty"] = false;
			}
		}

		private static void CustomizeCrystalHeart(OuiChapterPanel panel) {
			// customize heart gem icon
			string animId = null;

			// our sprite ID will be "AltSidesHelper_<heart sprite path keyified>"
			// log duplicate entries for a map 
			AltSidesHelperMeta parentHelperMeta = GetMetaForAreaData(AreaData.Get(GetMetaForAreaData(AreaData.Get(panel.Area))?.AltSideData?.For));
			if(parentHelperMeta != null)
				foreach(var mode in parentHelperMeta.Sides)
					if(mode.Map.Equals(panel.Area.SID))
						animId = mode.ChapterPanelHeartIcon.DialogKeyify();

			if(animId != null) {
				if(HeartSpriteBank.Has(animId)) {
					Sprite heartSprite = HeartSpriteBank.Create(animId);
					var selfdata = new DynData<OuiChapterPanel>(panel);
					var oldheart = selfdata.Get<HeartGemDisplay>("heart");
					oldheart.Sprites[0] = heartSprite;
					heartSprite.CenterOrigin();
					heartSprite.Play("spin");
					heartSprite.Visible = false;
					selfdata["AsHeartDirty"] = true;
				}
			}
		}

		private void SetPoemColour(On.Celeste.Poem.orig_ctor orig, Poem self, string text, int heartIndex, float heartAlpha) {
			orig(self, text, heartIndex, heartAlpha);
			// customize heart gem icon
			string animId = null;

			// our sprite ID will be "AltSidesHelper_<heart sprite path keyified>"
			// log duplicate entries for a map 
			var sid = (Engine.Scene as Level).Session.Area.SID;
			Color? color = null;
			AltSidesHelperMeta parentHelperMeta = GetMetaForAreaData(AreaData.Get(GetMetaForAreaData(AreaData.Get(sid))?.AltSideData?.For));
			if(parentHelperMeta != null)
				foreach(var mode in parentHelperMeta.Sides)
					if(mode.Map.Equals(sid)) {
						animId = mode.ChapterPanelHeartIcon.DialogKeyify();
						if(!mode.PoemDisplayColor.Equals(""))
							color = Calc.HexToColor(mode.PoemDisplayColor);
					}

			if(animId != null)
				if(HeartSpriteBank.Has(animId)) {
					HeartSpriteBank.CreateOn(self.Heart, animId);
					self.Heart.Play("spin");
					self.Heart.CenterOrigin();
				}
			if(color != null)
				new DynData<Poem>(self)["Color"] = color;
		}

		private bool FixReturnFromAltSide(On.Celeste.OuiChapterPanel.orig_IsStart orig, OuiChapterPanel self, Overworld overworld, Overworld.StartMode start) {
			AreaData newArea = null;
			if(start == Overworld.StartMode.AreaComplete || start == Overworld.StartMode.AreaQuit) {
				AreaData area = AreaData.Get(SaveData.Instance.LastArea.ID);
				var old = area;
				var meta = GetMetaForAreaData(area);
				if(meta?.AltSideData.IsAltSide ?? false) {
					area = AreaData.Get(meta.AltSideData.For) ?? area;
					if(area != null) {
						newArea = area;
						SaveData.Instance.LastArea.ID = area.ID;
						int returningSide = 0; //last unlocked mode
						if(!area.Interlude_Safe && area.HasMode(AreaMode.BSide) && (SaveData.Instance.Areas_Safe[area.ID].Cassette || SaveData.Instance.DebugMode || SaveData.Instance.CheatMode))
							returningSide++;
						if(!area.Interlude_Safe && area.HasMode(AreaMode.CSide) && SaveData.Instance.UnlockedModes >= 3)
							returningSide++;

						var asideAltSideMeta = GetMetaForAreaData(area);
						foreach(var mode in asideAltSideMeta.Sides)
							if(!mode.OverrideVanillaSideData) {
								returningSide++;
								if(mode.Map.Equals(area.GetSID()))
									break;
							}

						returningAltSide = returningSide;
						SaveData.Instance.LastArea_Safe.ID = old.ID;
					}
				}
			}
			var ret = orig(self, overworld, start);
			if(newArea != null) {
				//self.Data = newArea;
				SaveData.Instance.LastArea_Safe.ID = newArea.ID;
				shouldResetStats = false;
				resetMethod.Invoke(self, new object[] { });
				shouldResetStats = true;
				overworld.Mountain.SnapState(self.Data.MountainState);
				overworld.Mountain.SnapCamera(self.Area.ID, self.Data.MountainZoom);
				overworld.Mountain.EaseCamera(self.Area.ID, self.Data.MountainSelect, 1f, true);
			}
			returningAltSide = -1;

			return ret;
		}

		private void FixSettingAltSideStats(On.Celeste.OuiChapterPanel.orig_UpdateStats orig, OuiChapterPanel self, bool wiggle, bool? overrideStrawberryWiggle, bool? overrideDeathWiggle, bool? overrideHeartWiggle) {
			if(shouldResetStats)
				orig(self, wiggle, overrideStrawberryWiggle, overrideDeathWiggle, overrideHeartWiggle);
		}

		private void FixSettingAltSideStatPositions(On.Celeste.OuiChapterPanel.orig_SetStatsPosition orig, OuiChapterPanel self, bool approach) {
			if(shouldResetStats)
				orig(self, approach);
		}

		private static void AddExtraModes(OuiChapterPanel self) {
			// check map meta for extra sides or side overrides
			AltSidesHelperMeta meta = new DynData<AreaData>(self.Data).Get<AltSidesHelperMeta>("AltSidesHelperMeta");
			if(meta?.Sides != null) {
				int siblings = ((IList)modesField.GetValue(self)).Count;
				int oldModes = siblings;
				// find the new total number of modes
				foreach(var mode in meta.Sides)
					if(!mode.OverrideVanillaSideData)
						siblings++;
				// adjust the original options to fit, and attach the map path & mode to the original options
				int origMode = 0;
				foreach(var vmode in (IList)modesField.GetValue(self)) {
					DynamicData data = new DynamicData(vmode);
					if(siblings > 5) {
						data.Set("Siblings", siblings);
						data.Set("Large", false);
					}
					data.Set("AreaKey", self.Data.ToKey((AreaMode)origMode));
					origMode++;
				}

				// apply mode settings
				for(int i = 0; i < meta.Sides.Length; i++) {
					AltSidesHelperMode mode = meta.Sides[i];
					if(!mode.OverrideVanillaSideData) {
						object newOptn;
						((IList)modesField.GetValue(self)).Add(
							newOptn = DynamicData.New(t_OuiChapterPanelOption)(new {
								Label = Dialog.Clean(mode.Label),
								Icon = GFX.Gui[mode.Icon],
								ID = "AltSidesHelperMode_" + i.ToString(),
								Siblings = siblings > 5 ? siblings : 0
							})
						);
						DynamicData data = new DynamicData(newOptn);
						AreaData map = null;
						foreach(var area in AreaData.Areas)
							if(area.SID.Equals(mode.Map))
								map = area;
						data.Set("AreaKey", map.ToKey());
					} else {
						// find the vanilla mode and modify it
						// IsAltSide is handled elsewhere
						foreach(var vmode in (IList)modesField.GetValue(self)) {
							DynamicData data = new DynamicData(vmode);
							// ...
						}
					}
				}
			}

			int count = ((IList)modesField.GetValue(self)).Count;
			for(int i = 0; i < count; i++) {
				//DynamicData data = new DynamicData(((IList)modesField.GetValue(self))[i]);
				//data.Invoke("SlideTowards", count, i);
			}
		}

		private bool ReturnToAltSide(On.Celeste.OuiChapterSelect.orig_IsStart orig, OuiChapterSelect self, Overworld overworld, Overworld.StartMode start) {
			if(start == Overworld.StartMode.AreaComplete || start == Overworld.StartMode.AreaQuit) {
				AreaData area = AreaData.Get(SaveData.Instance.LastArea.ID);
				var meta = GetMetaForAreaData(area);
				if(meta?.AltSideData.IsAltSide ?? false) {
					area = AreaData.Get(meta.AltSideData.For) ?? area;
					if(area != null)
						SaveData.Instance.LastArea.ID = area.ID;
				}
			}
			
			return orig(self, overworld, start);
		}

		private delegate void orig_OuiChapterPanel_set_option(OuiChapterPanel self, int option);
		private static void OnChapterPanelChangeOption(orig_OuiChapterPanel_set_option orig, OuiChapterPanel self, int option) {
			orig(self, option);
			var data = new DynData<OuiChapterPanel>(self);
			if(data.Get<bool>("selectingMode")) {
				data["TrueMode"] = option;

				UpdateDataForTrueMode(self, option);
				ResetCrystalHeart(self);
				CustomizeCrystalHeart(self);
			}
		}

		private static void UpdateDataForTrueMode(OuiChapterPanel self, int option) {
			try {
				self.Area = new DynamicData(((IList)modesField.GetValue(self))[option]).Get<AreaKey>("AreaKey");
				self.RealStats = SaveData.Instance.Areas_Safe[self.Area.ID];
				self.DisplayedStats = self.RealStats;
				self.Data = AreaData.Areas[self.Area.ID];
			} catch(NullReferenceException) { }
		}

		private delegate int orig_OuiChapterPanel_get_option(OuiChapterPanel self);
		private static int OnChapterPanelGetOption(orig_OuiChapterPanel_get_option orig, OuiChapterPanel self) {
			var data = new DynData<OuiChapterPanel>(self);
			if(data.Get<bool>("selectingMode")) {
				try {
					return Math.Min(data.Get<int>("TrueMode"), ((IList)modesField.GetValue(self)).Count - 1);
				} catch {
					return orig(self);
				}
			} else return orig(self);
		}

		private delegate int orig_OuiChapterSelect_get_area(OuiChapterSelect self);
		private static int OnChapterSelectGetArea(orig_OuiChapterSelect_get_area orig, OuiChapterSelect self) {
			int prevArea = orig(self);
			var meta = GetMetaForAreaData(AreaData.Areas[prevArea]);
			if(meta?.AltSideData.IsAltSide ?? false) {
				prevArea = AreaData.Areas.IndexOf(AreaData.Get(meta.AltSideData.For));
			}
			return prevArea;
		}

		private void PostAreaLoad(On.Celeste.AreaData.orig_Load orig) {
			orig();
			var heartTextures = new HashSet<string>();
			foreach(var map in AreaData.Areas) {
				// Load "mapdir/mapname.altsideshelper.meta.yaml" as a AltSidesHelperMeta
				AltSidesHelperMeta meta;
				if(Everest.Content.TryGet("Maps/" + map.Mode[0].Path + ".altsideshelper.meta", out ModAsset metadata) && metadata.TryDeserialize(out meta)) {
					foreach(var mode in meta.Sides) {
						mode.ApplyPreset();
						heartTextures.Add(mode.ChapterPanelHeartIcon);
					}
					// Attach the meta to the AreaData w/ DynData
					DynData<AreaData> areaDynData = new DynData<AreaData>(map);
					areaDynData["AltSidesHelperMeta"] = meta;
					if(meta.AltSideData.IsAltSide) {
						var aside = AreaData.Get(meta.AltSideData.For);
						if(meta.AltSideData.CopyEndScreenData)
							map.Meta.CompleteScreen = aside.Meta.CompleteScreen;
						if(meta.AltSideData.CopyMountainData)
							map.Meta.Mountain = aside.Meta.Mountain;
					}
				}
			}

			SpriteBank crystalHeartSwaps = new SpriteBank(GFX.Gui, "Graphics/AltSidesHelper/Empty.xml");

			// make a fake XML doc to store our textures
			// TODO: allow using real XMLs too - load them and copy them in
			XmlDocument assetXml = new XmlDocument();
			var spritesRoot = assetXml.CreateChild("Sprites");
			
			foreach(var heart in heartTextures) {
				// we're going to assume the following XML:
				/*
				   <heartgem1 path="collectables/heartgem/1/" start="idle">
					<Center />
					<Loop id="idle" path="spin" frames="0" />
					<Loop id="spin" path="spin" frames="0*10,1-10" delay="0.08"/>
					<Loop id="fastspin" path="spin" frames="1-10" delay="0.08"/>
				  </heartgem1>
				*/
				// our sprite ID will be "<heart sprite path keyified>"
				var sprite = spritesRoot.CreateChild(heart.DialogKeyify());
				// we're talking along the lines of "collectables/heartgem/0/spin"
				// use the last part of the name as the loop paths, and the rest as element path

				var parts = heart.Split('/');
				var loopPath = parts[parts.Length - 1];
				string elemPath = heart.Substring(0, heart.Length - loopPath.Length);

				// TODO: replace with what we do for in-world hearts
				sprite.SetAttribute("path", elemPath);
				sprite.SetAttribute("start", "idle");
				var idle = sprite.CreateChild("Loop");
				idle.SetAttribute("id", "idle");
				idle.SetAttribute("path", loopPath);
				idle.SetAttribute("frames", "0");
				var spin = sprite.CreateChild("Loop");
				spin.SetAttribute("id", "spin");
				spin.SetAttribute("path", loopPath);
				spin.SetAttribute("frames", "0*10,1-10");
				spin.SetAttribute("delay", "0.08");
				var fastspin = sprite.CreateChild("Loop");
				fastspin.SetAttribute("id", "fastspin");
				fastspin.SetAttribute("path", loopPath);
				fastspin.SetAttribute("frames", "1-10");
				fastspin.SetAttribute("delay", "0.08");
			}
			SpriteBank newSpriteBank = new SpriteBank(GFX.Gui, assetXml);

			int hearts = 0;
			foreach(var kvp in newSpriteBank.SpriteData) {
				hearts++;
				crystalHeartSwaps.SpriteData[kvp.Key] = kvp.Value;
			}

			HeartSpriteBank = crystalHeartSwaps;
			Logger.Log("AltSidesHelper", $"Loaded {hearts} crystal heart UI textures.");
		}

		private void HideAltSides(On.Celeste.OuiChapterSelect.orig_Added orig, OuiChapterSelect self, Scene scene) {
			orig(self, scene);
			var icons = new DynData<OuiChapterSelect>(self).Get<List<OuiChapterSelectIcon>>("icons");
			for(int i = icons.Count - 1; i >= 0; i--) {
				var meta = GetMetaForAreaData(AreaData.Get(icons[i].Area));
				if(meta?.AltSideData.IsAltSide ?? false) {
					icons[i].Area = -1;
					icons[i].Hide();
				}
			}
		}

		public static AltSidesHelperMeta GetMetaForAreaData(AreaData data){
			if(data == null)
				return null;
			return new DynData<AreaData>(data).Get<AltSidesHelperMeta>("AltSidesHelperMeta");
		}

		public static AltSidesHelperMode GetModeMetaForAltSide(AreaData data) {
			if(data == null)
				return null;
			AltSidesHelperMeta parentHelperMeta = GetMetaForAreaData(AreaData.Get(GetMetaForAreaData(AreaData.Get(data.SID))?.AltSideData?.For));
			if(parentHelperMeta != null)
				foreach(var mode in parentHelperMeta.Sides)
					if(mode.Map.Equals(data.SID))
						return mode;
			return null;
		}
	}

	public class AltSidesHelperMeta {

		public AltSidesHelperMode[] Sides {
			get;
			set;
		} = new AltSidesHelperMode[0];

		public AltSideMeta AltSideData {
			get;
			set;
		} = new AltSideMeta();
	}

	public class AltSideMeta {
		public bool IsAltSide {
			get;
			set;
		} = false;

		public string For {
			get;
			set;
		} = "";

		public bool CopyMountainData {
			get;
			set;
		} = true;

		public bool CopyEndScreenData {
			get;
			set;
		} = true;
	}

	public class AltSidesHelperMode {

		public string Map {
			get;
			set;
		}
		public string Preset {
			get;
			set;
		} = "none";

		// Dialog key
		public string Label {
			get;
			set;
		} = "";

		// Relative to Atlases/Gui
		public string Icon {
			get;
			set;
		} = "";
		public string DeathsIcon {
			get;
			set;
		} = "";
		public string ChapterPanelHeartIcon {
			get;
			set;
		} = "";

		// Relative to Atlases/Gameplay
		public string InWorldHeartIcon {
			get;
			set;
		} = "";

		// Relative to Atlases/Journal
		public string JournalHeartIcon {
			get;
			set;
		} = "";

		// Hex colour code
		public string PoemDisplayColor {
			get;
			set;
		} = "";

		// For overriding vanilla side data
		public bool OverrideVanillaSideData {
			get;
			set;
		} = false;
		public string VanillaSide {
			get;
			set;
		} = "";

		public void ApplyPreset() {
			if(Preset.Equals("a-side")){
				Label = "OVERWORLD_NORMAL";
				Icon = "menu/play";
				DeathsIcon = "collectables/skullBlue";
				ChapterPanelHeartIcon = "collectables/heartgem/0/spin";
				InWorldHeartIcon = "collectables/heartGem/0/";
				JournalHeartIcon = "heartgem0";
				PoemDisplayColor = "8cc7fa";
			} else if(Preset.Equals("b-side")) {
				Label = "OVERWORLD_REMIX";
				Icon = "menu/remix";
				DeathsIcon = "collectables/skullRed";
				ChapterPanelHeartIcon = "collectables/heartgem/1/spin";
				InWorldHeartIcon = "collectables/heartGem/1/";
				JournalHeartIcon = "heartgem1";
				PoemDisplayColor = "ff668a";
			} else if(Preset.Equals("c-side")) {
				Label = "OVERWORLD_REMIX2";
				Icon = "menu/rmx2";
				DeathsIcon = "collectables/skullGold";
				ChapterPanelHeartIcon = "collectables/heartgem/2/spin";
				InWorldHeartIcon = "collectables/heartGem/2/";
				JournalHeartIcon = "heartgem2";
				PoemDisplayColor = "fffc24";
			} else if(Preset.Equals("d-side")) {
				// TODO: Missing icons
				Label = "leppa_AltSidesHelper_overworld_remix3";
				Icon = "menu/leppa/AltSidesHelper/rmx3";
				DeathsIcon = "collectables/skullGold";
				ChapterPanelHeartIcon = "collectables/leppa/AltSidesHelper/heartgem/dside";
				InWorldHeartIcon = "collectables/heartGem/3/";
				JournalHeartIcon = "heartgem2";
				PoemDisplayColor = "ffffff";
			}
		}
	}
}
