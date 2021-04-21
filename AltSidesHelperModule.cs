using Celeste.Mod;
using Celeste;
using System.Reflection;
using System;
using System.Collections;
using MonoMod.Utils;
using MonoMod.RuntimeDetour;
using System.Collections.Generic;
using Monocle;

namespace AltSidesHelper {
	public class AltSidesHelperModule : EverestModule {

		public static AltSidesHelperModule Instance;
		private static IDetour hook_OuiChapterPanel_set_option;
		private static IDetour hook_OuiChapterPanel_get_option;
		private static IDetour hook_OuiChapterSelect_get_area;

		private int returningAltSide = -1;

		private static readonly Type t_OuiChapterPanelOption = typeof(OuiChapterPanel)
			.GetNestedType("Option", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		private static FieldInfo modesField = typeof(OuiChapterPanel)
			.GetField("modes", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
		private static MethodInfo updateStatsMethod = typeof(OuiChapterPanel)
			.GetMethod("UpdateStats", BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.Instance);

		public AltSidesHelperModule() {
			Instance = this;
		}

		public override void Load() {
			On.Celeste.OuiChapterPanel.Reset += OnChapterPanelReset;
			On.Celeste.OuiChapterPanel.IsStart += OnChapterPanelIsStart;
			On.Celeste.AreaData.Load += OnAreaDataLoad;
			On.Celeste.OuiChapterSelect.Added += OnChapterSelectAdded;
			On.Celeste.OuiChapterSelect.IsStart += OnChapterSelectIsStart;

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
			On.Celeste.OuiChapterPanel.Reset -= OnChapterPanelReset;
			On.Celeste.OuiChapterPanel.IsStart -= OnChapterPanelIsStart;
			On.Celeste.AreaData.Load -= OnAreaDataLoad;
			On.Celeste.OuiChapterSelect.Added -= OnChapterSelectAdded;
			On.Celeste.OuiChapterSelect.IsStart -= OnChapterSelectIsStart;

			//IL.Celeste.OuiJournalProgress.ctor -= OnJournalProgressPageConstruct;

			hook_OuiChapterPanel_set_option.Dispose();
			hook_OuiChapterPanel_get_option.Dispose();
			hook_OuiChapterSelect_get_area.Dispose();
		}

		private void OnChapterPanelReset(On.Celeste.OuiChapterPanel.orig_Reset orig, OuiChapterPanel self) {
			orig(self);

			// check if we're returning from an alt-side
			var selfdata = new DynData<OuiChapterPanel>(self);
			if(returningAltSide == -1) {
				selfdata.Data.Remove("TrueMode");
			} else {
				selfdata["TrueMode"] = returningAltSide;
				returningAltSide = -1;
			}

			// check map meta for extra sides or side overrides
			AltSidesHelperMeta meta = new DynData<AreaData>(self.Data).Get<AltSidesHelperMeta>("DSidesHelperMeta");
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
							newOptn = (DynamicData.New(t_OuiChapterPanelOption)(new {
								Label = Dialog.Clean(mode.Label),
								Icon = GFX.Gui[mode.Icon],
								ID = "DSidesHelperMode_" + i.ToString(),
								Siblings = siblings > 5 ? siblings : 0
							}))
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
							DynData<object> data = new DynData<object>(vmode);
							// ...
						}
					}
				}
			}

			// ensure that, when returning from an alt-side, stats are displayed properly
			try {
				self.Area = new DynamicData(((IList)modesField.GetValue(self))[(int)selfdata["TrueMode"]]).Get<AreaKey>("AreaKey");
				self.RealStats = SaveData.Instance.Areas_Safe[self.Area.ID];
				self.DisplayedStats = self.RealStats;
				// self.Data = AreaData.Areas[self.Area.ID]; // don't set this, or the mountain position of the alt-side is used
			} catch(NullReferenceException) { }
			updateStatsMethod.Invoke(self, new object[] { false, null, null, null });
		}

		private bool OnChapterPanelIsStart(On.Celeste.OuiChapterPanel.orig_IsStart orig, OuiChapterPanel self, Overworld overworld, Overworld.StartMode start) {
			if(start == Overworld.StartMode.AreaComplete || start == Overworld.StartMode.AreaQuit) {
				AreaData area = AreaData.Get(SaveData.Instance.LastArea.ID);
				var meta = GetMetaForAreaData(area);
				if(meta?.AltSideData.IsAltSide ?? false) {
					area = AreaData.Get(meta.AltSideData.For) ?? area;
					if(area != null) {
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
					}
				}
			}
			return orig(self, overworld, start);
		}

		private bool OnChapterSelectIsStart(On.Celeste.OuiChapterSelect.orig_IsStart orig, OuiChapterSelect self, Overworld overworld, Overworld.StartMode start) {
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

				try {
					self.Area = new DynamicData(((IList)modesField.GetValue(self))[option]).Get<AreaKey>("AreaKey");
					self.RealStats = SaveData.Instance.Areas_Safe[self.Area.ID];
					self.DisplayedStats = self.RealStats;
					self.Data = AreaData.Areas[self.Area.ID];
				} catch(NullReferenceException) { }
			}
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

		private void OnAreaDataLoad(On.Celeste.AreaData.orig_Load orig) {
			orig();
			foreach(var map in AreaData.Areas) {
				// Load "mapdir/mapname.dsideshelper.meta.yaml" as a DSidesHelperMode[]
				AltSidesHelperMeta meta;
				if(Everest.Content.TryGet("Maps/" + map.Mode[0].Path + ".dsideshelper.meta", out ModAsset metadata) && metadata.TryDeserialize(out meta)) {
					foreach(var mode in meta.Sides)
						mode.ApplyPreset();
					// Attach the meta to the AreaData w/ DynData
					DynData<AreaData> areaDynData = new DynData<AreaData>(map);
					areaDynData["DSidesHelperMeta"] = meta;
					if(meta.AltSideData.IsAltSide) {
						var aside = AreaData.Get(meta.AltSideData.For);
						if(meta.AltSideData.CopyEndScreenData)
							map.Meta.CompleteScreen = aside.Meta.CompleteScreen;
						if(meta.AltSideData.CopyMountainData)
							map.Meta.Mountain = aside.Meta.Mountain;
					}
				}
			}
		}

		private void OnChapterSelectAdded(On.Celeste.OuiChapterSelect.orig_Added orig, OuiChapterSelect self, Scene scene) {
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
			return new DynData<AreaData>(data).Get<AltSidesHelperMeta>("DSidesHelperMeta");
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
				InWorldHeartIcon = "collectables/heartGem/0";
				JournalHeartIcon = "heartgem0";
			} else if(Preset.Equals("b-side")) {
				Label = "OVERWORLD_REMIX";
				Icon = "menu/remix";
				DeathsIcon = "collectables/skullRed";
				ChapterPanelHeartIcon = "collectables/heartgem/1/spin";
				InWorldHeartIcon = "collectables/heartGem/1";
				JournalHeartIcon = "heartgem1";
			} else if(Preset.Equals("c-side")) {
				Label = "OVERWORLD_REMIX2";
				Icon = "menu/rmx2";
				DeathsIcon = "collectables/skullGold";
				ChapterPanelHeartIcon = "collectables/heartgem/2/spin";
				InWorldHeartIcon = "collectables/heartGem/2";
				JournalHeartIcon = "heartgem2";
			} else if(Preset.Equals("d-side")) {
				// TODO: Missing icons
				Label = "leppa_DSidesHelper_overworld_remix3";
				Icon = "menu/leppa/DSidesHelper/rmx3";
				DeathsIcon = "collectables/skullBlue";
				ChapterPanelHeartIcon = "collectables/leppa/DSidesHelper/heartgem/dside";
				InWorldHeartIcon = "collectables/heartGem/3";
				JournalHeartIcon = "heartgem0";
			}
		}
	}
}
