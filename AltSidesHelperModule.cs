using Celeste.Mod;
using Celeste;
using System.Reflection;
using System;
using System.Collections;
using MonoMod.Utils;
using MonoMod.RuntimeDetour;
using System.Collections.Generic;
using Monocle;
using Microsoft.Xna.Framework;
using System.Linq;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace AltSidesHelper {
	public class AltSidesHelperModule : EverestModule {

		public static AltSidesHelperModule Instance;

		public override Type SaveDataType => typeof(AltSidesHelperSaveData);

		// save data - contains unlocked alt-sides by cassettes or triggers
		public static AltSidesHelperSaveData AltSidesSaveData => (AltSidesHelperSaveData)Instance._SaveData;

		// alt-sides metadata
		public static Dictionary<AreaData, AltSidesHelperMeta> AltSidesMetadata = new Dictionary<AreaData, AltSidesHelperMeta>();

		// hooks
		private static IDetour hook_OuiChapterPanel_set_option;
		private static IDetour hook_OuiChapterPanel_get_option;
		private static IDetour hook_OuiChapterSelect_get_area;
		private static IDetour hook_LevelSetStats_get_MaxArea;
		private static IDetour hook_Session_get_FullClear;

		private static IDetour mod_OuiFileSelectSlot_orig_Render;

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

		public AltSidesHelperModule() {
			Instance = this;
		}

		public override void Load() {
			On.Celeste.OuiChapterPanel.Reset += CustomiseChapterPanel;
			On.Celeste.OuiChapterPanel.IsStart += FixReturnFromAltSide;
			On.Celeste.OuiChapterPanel.UpdateStats += FixSettingAltSideStats;
			On.Celeste.OuiChapterPanel.SetStatsPosition += FixSettingAltSideStatPositions;
			On.Celeste.AreaData.Load += PostAreaLoad;
			On.Celeste.AreaData.AreaComparison += SortAltSidesLast;
			On.Celeste.OuiChapterSelect.Added += HideAltSides;
			On.Celeste.OuiChapterSelect.IsStart += ReturnToAltSide;
			On.Celeste.Poem.ctor += SetPoemColour;
			On.Celeste.DeathsCounter.SetMode += SetDeathsCounterIcon;
			On.Celeste.HeartGem.Awake += SetCrystalHeartSprite;
			On.Celeste.AreaComplete.GetCustomCompleteScreenTitle += SetAltSideEndScreenTitle;
			On.Celeste.LevelEnter.Routine += AddAltSideRemixTitle;

			IL.Celeste.OuiJournalProgress.ctor += ModJournalProgressPageConstruct;
			
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
			hook_LevelSetStats_get_MaxArea = new Hook(
				typeof(LevelSetStats).GetProperty("MaxArea", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(),
				typeof(AltSidesHelperModule).GetMethod("OnLevelSetStatsGetMaxArea", BindingFlags.NonPublic | BindingFlags.Static)
			);
			hook_Session_get_FullClear = new Hook(
				typeof(Session).GetProperty("FullClear", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(),
				typeof(AltSidesHelperModule).GetMethod("OnSessionGetFullClear", BindingFlags.NonPublic | BindingFlags.Static)
			);

			mod_OuiFileSelectSlot_orig_Render = new ILHook(
				typeof(OuiFileSelectSlot).GetMethod("orig_Render", BindingFlags.Public | BindingFlags.Instance),
				ModFileSelectSlotRender
			);

			Logger.SetLogLevel("AltSidesHelper", LogLevel.Info);
		}

		public override void LoadContent(bool firstLoad) {
			
		}

		public override void Unload() {
			On.Celeste.OuiChapterPanel.Reset -= CustomiseChapterPanel;
			On.Celeste.OuiChapterPanel.IsStart -= FixReturnFromAltSide;
			On.Celeste.OuiChapterPanel.UpdateStats -= FixSettingAltSideStats;
			On.Celeste.OuiChapterPanel.SetStatsPosition -= FixSettingAltSideStatPositions;
			On.Celeste.AreaData.Load -= PostAreaLoad;
			On.Celeste.AreaData.AreaComparison -= SortAltSidesLast;
			On.Celeste.OuiChapterSelect.Added -= HideAltSides;
			On.Celeste.OuiChapterSelect.IsStart -= ReturnToAltSide;
			On.Celeste.Poem.ctor -= SetPoemColour;
			On.Celeste.DeathsCounter.SetMode -= SetDeathsCounterIcon;
			On.Celeste.HeartGem.Awake -= SetCrystalHeartSprite;
			On.Celeste.AreaComplete.GetCustomCompleteScreenTitle -= SetAltSideEndScreenTitle;
			On.Celeste.LevelEnter.Routine -= AddAltSideRemixTitle;

			IL.Celeste.OuiJournalProgress.ctor -= ModJournalProgressPageConstruct;

			hook_OuiChapterPanel_set_option.Dispose();
			hook_OuiChapterPanel_get_option.Dispose();
			hook_OuiChapterSelect_get_area.Dispose();
			hook_LevelSetStats_get_MaxArea.Dispose();
			hook_Session_get_FullClear.Dispose();
			mod_OuiFileSelectSlot_orig_Render.Dispose();

			AltSidesMetadata.Clear();
		}

		public override void DeserializeSaveData(int index, byte[] data) {
			base.DeserializeSaveData(index, data);
			if(AltSidesSaveData == null)
				Instance._SaveData = new AltSidesHelperSaveData();
			if(AltSidesSaveData.UnlockedAltSideIDs == null)
				AltSidesSaveData.UnlockedAltSideIDs = new HashSet<string>();
		}

		private void ModFileSelectSlotRender(ILContext il) {
			ILCursor cursor = new ILCursor(il);
			if(cursor.TryGotoNext(MoveType.After,
								instr => instr.Match(OpCodes.Box),
								instr => instr.MatchCall<string>("Concat"),
								instr => instr.MatchCallvirt<Atlas>("get_Item"))) {
				Logger.Log(LogLevel.Info, "AltSidesHelper", $"Modding file select slot at {cursor.Index} in IL for OuiFileSelectSlot.orig_Render.");
				cursor.Emit(OpCodes.Ldarg_0);
				cursor.Emit(OpCodes.Ldfld, typeof(OuiFileSelectSlot).GetField("SaveData"));
				cursor.Emit(OpCodes.Ldloc_S, il.Method.Body.Variables[11]);
				cursor.EmitDelegate<Func<MTexture, SaveData, int, MTexture>>((orig, save, index) => {
					var levelset = save.LevelSet;
					AreaData data = null; int i = 0;
					foreach(var item in AreaData.Areas) {
						if(item.GetLevelSet().Equals(levelset)) {
							if(i == index) {
								data = item;
								break;
							}
							i++;
						}
					}
					if(data != null) {
						var meta = GetModeMetaForAltSide(data);
						if(meta != null && meta.OverrideHeartTextures) {
							Logger.Log("AltSidesHelper", $"Changing file select heart texture for \"{data.SID}\".");
							// use *our* gem
							return MTN.Journal[meta.JournalHeartIcon];
						}
					}
					return orig;
				});
			}
		}

		private void ModJournalProgressPageConstruct(ILContext il) {
			ILCursor cursor = new ILCursor(il);
			if(cursor.TryGotoNext(MoveType.After,
								instr => instr.Match(OpCodes.Box),
								instr => instr.MatchCall<string>("Concat"))) {
				// now do that again :P
				if(cursor.TryGotoNext(MoveType.After,
								instr => instr.Match(OpCodes.Box),
								instr => instr.MatchCall<string>("Concat"))) {
					Logger.Log(LogLevel.Info, "AltSidesHelper", $"Modding journal progress page at {cursor.Index} in IL for OuiJournalProgress constructor.");
					cursor.Emit(OpCodes.Ldloc_2); // data
					cursor.EmitDelegate<Func<string, AreaData, string>>((orig, data) => {
						var meta = GetModeMetaForAltSide(data);
						if(meta != null && meta.OverrideHeartTextures) {
							Logger.Log("AltSidesHelper", $"Changing journal heart colour for \"{data.SID}\".");
							// use *our* gem
							return meta.JournalHeartIcon;
						}
						return orig;
					});
				}
			}
			if(cursor.TryGotoNext(MoveType.After,
								instr => instr.MatchLdstr("cassette"),
								instr => instr.MatchStelemRef(),
								instr => instr.MatchNewobj<OuiJournalPage.IconsCell>())) {
				cursor.Emit(OpCodes.Ldloc_2); // data
				cursor.EmitDelegate<Func<OuiJournalPage.IconsCell, AreaData, OuiJournalPage.IconsCell>>((orig, data) => {
					var meta = GetMetaForAreaData(data);
					if(meta != null) {
						DynData<OuiJournalPage.IconsCell> dyn = new DynData<OuiJournalPage.IconsCell>(orig);
						List<string> cassettes = new List<string>();
						if(string.Equals(dyn.Get<string[]>("icons")[0], "cassette")) {
							cassettes.Add("cassette");
						}
						foreach(var item in meta.Sides) {
							if(!item.OverrideVanillaSideData && item.AddCassetteIcon && AltSidesSaveData.UnlockedAltSideIDs.Contains(item.Map)) {
								cassettes.Add(item.JournalCassetteIcon);
							}
						}
						if(cassettes.Count == 0) {
							cassettes.Add("dot");
						}
						dyn["icons"] = cassettes.ToArray();
						dyn["iconSpacing"] = -40f;
					}
					return orig;
				});
			}
		}

		private int SortAltSidesLast(On.Celeste.AreaData.orig_AreaComparison orig, AreaData a, AreaData b) {
			if(!string.IsNullOrEmpty(GetMetaForAreaData(a)?.AltSideData?.For) && string.IsNullOrEmpty(GetMetaForAreaData(b)?.AltSideData?.For))
				return 1;
			if(string.IsNullOrEmpty(GetMetaForAreaData(a)?.AltSideData?.For) && !string.IsNullOrEmpty(GetMetaForAreaData(b)?.AltSideData?.For))
				return -1;
			return orig(a, b);
		}

		private string SetAltSideEndScreenTitle(On.Celeste.AreaComplete.orig_GetCustomCompleteScreenTitle orig, AreaComplete self) {
			var ret = orig(self);
			var data = AreaData.Get(self.Session.Area);
			var meta = GetModeMetaForAltSide(data);
			if(meta != null) {
				Logger.Log("AltSidesHelper", $"Replacing end screen title for \"{data.SID}\".");
				if(self.Session.FullClear) {
					return Dialog.Clean(meta.EndScreenClearTitle);
				}

				if(!meta.EndScreenTitle.Equals(""))
					return Dialog.Clean(meta.EndScreenTitle);
			}
			return ret;
		}

		private void SetCrystalHeartSprite(On.Celeste.HeartGem.orig_Awake orig, HeartGem self, Scene scene) {
			orig(self, scene);
			if(!self.IsFake) {
				var data = AreaData.Get((scene as Level).Session.Area);
				var meta = GetModeMetaForAltSide(data);
				if(meta != null) {
					Logger.Log("AltSidesHelper", $"In-world heart customisation: found metadata for \"{data.SID}\".");
					if(meta.OverrideHeartTextures) {
						Logger.Log("AltSidesHelper", $"Replacing crystal heart texture for \"{data.SID}\".");
						var selfdata = new DynData<HeartGem>(self);
						if(!self.IsGhost) {
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

						var colour = Calc.HexToColor(meta.HeartColour);
						selfdata["shineParticle"] = new ParticleType(HeartGem.P_BlueShine) {
							Color = colour
						};

						selfdata.Get<VertexLight>("light").RemoveSelf();
						var newLight = new VertexLight(Color.Lerp(colour, Color.White, 0.5f), 1f, 32, 64);
						self.Add(newLight);
						selfdata["light"] = newLight;
					}
				}
			}
		}

		private void SetDeathsCounterIcon(On.Celeste.DeathsCounter.orig_SetMode orig, DeathsCounter self, AreaMode mode) {
			orig(self, mode);
			if(self.Entity is OuiChapterPanel panel) {
				var meta = GetModeMetaForAltSide(panel.Data);
				if(meta != null) {
					Logger.Log("AltSidesHelper", $"Replacing deaths icon for \"{panel.Data.SID}\".");
					new DynData<DeathsCounter>(self).Set("icon", GFX.Gui[meta.DeathsIcon]);
				}
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
				Logger.Log("AltSidesHelper", $"Resetting dirty crystal heart for {panel.Data.SID}.");
			}
		}

		private static void CustomizeCrystalHeart(OuiChapterPanel panel) {
			// customize heart gem icon
			string animId = null;

			// our sprite ID will be "AltSidesHelper_<heart sprite path keyified>"
			var data = AreaData.Get(panel.Area);
			AltSidesHelperMode mode = GetModeMetaForAltSide(data);
			if(mode != null) {
				Logger.Log("AltSidesHelper", $"Found meta for \"{data.SID}\" when customising UI heart.");
				if(mode.OverrideHeartTextures) {
					animId = mode.ChapterPanelHeartIcon.DialogKeyify();
					Logger.Log("AltSidesHelper", $"Will change UI heart sprite for \"{data.SID}\".");
				}
			}

			if(animId != null) {
				if(HeartSpriteBank.Has(animId)) {
					Logger.Log("AltSidesHelper", $"Replacing UI heart sprite for \"{data.SID}\".");
					Sprite heartSprite = HeartSpriteBank.Create(animId);
					var selfdata = new DynData<OuiChapterPanel>(panel);
					var oldheart = selfdata.Get<HeartGemDisplay>("heart");
					bool prevVisible = oldheart.Sprites[0].Visible;
					oldheart.Sprites[0] = heartSprite;
					heartSprite.CenterOrigin();
					heartSprite.Play("spin");
					heartSprite.Visible = prevVisible || oldheart.Sprites[1].Visible || oldheart.Sprites[2].Visible;
					selfdata["AsHeartDirty"] = true;
				}
			}
		}

		private void SetPoemColour(On.Celeste.Poem.orig_ctor orig, Poem self, string text, int heartIndex, float heartAlpha) {
			var data = AreaData.Get((Engine.Scene as Level).Session.Area);
			var m = GetModeMetaForAltSide(data);
			if(data != null) {
				Logger.Log("AltSidesHelper", $"Customising poem UI for \"{data.SID}\".");
			}
			if (!(m?.ShowHeartPoem) ?? false)
				text = null;
			orig(self, text, heartIndex, heartAlpha);
			// customize heart gem icon
			string animId = null;

			// our sprite ID will be "AltSidesHelper_<heart sprite path keyified>"
			// log duplicate entries for a map 
			var sid = (Engine.Scene as Level).Session.Area.SID;
			Color? color = null;
			AltSidesHelperMode mode = GetModeMetaForAltSide(AreaData.Get(sid));
			if(mode != null && mode.OverrideHeartTextures) {
				animId = mode.ChapterPanelHeartIcon.DialogKeyify();
				if(!mode.HeartColour.Equals("")) {
					color = Calc.HexToColor(mode.HeartColour);
				}
			}

			if(animId != null)
				if(HeartSpriteBank.Has(animId)) {
					HeartSpriteBank.CreateOn(self.Heart, animId);
					self.Heart.Play("spin");
					self.Heart.CenterOrigin();
					Logger.Log("AltSidesHelper", $"Changed poem heart sprite for \"{data.SID}\".");
				}
			if(color != null) {
				new DynData<Poem>(self)["Color"] = color;
				Logger.Log("AltSidesHelper", $"Changed poem colour for \"{data.SID}\".");
			}
		}

		private bool FixReturnFromAltSide(On.Celeste.OuiChapterPanel.orig_IsStart orig, OuiChapterPanel self, Overworld overworld, Overworld.StartMode start) {
			AreaData newArea = null;
			AreaData old;
			if(start == Overworld.StartMode.AreaComplete || start == Overworld.StartMode.AreaQuit) {
				AreaData area = AreaData.Get(SaveData.Instance.LastArea.ID);
				old = area;
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
								if(mode.Map.Equals(old.GetSID()))
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
			}
			returningAltSide = -1;

			return ret;
		}

		private void FixSettingAltSideStats(On.Celeste.OuiChapterPanel.orig_UpdateStats orig, OuiChapterPanel self, bool wiggle, bool? overrideStrawberryWiggle, bool? overrideDeathWiggle, bool? overrideHeartWiggle) {
			if(shouldResetStats) {
				orig(self, wiggle, overrideStrawberryWiggle, overrideDeathWiggle, overrideHeartWiggle);
				if(GetModeMetaForAltSide(self.Data)?.ShowBerriesAsGolden ?? false)
					new DynData<OuiChapterPanel>(self).Get<StrawberriesCounter>("strawberries").Golden = true;
			}
		}

		private void FixSettingAltSideStatPositions(On.Celeste.OuiChapterPanel.orig_SetStatsPosition orig, OuiChapterPanel self, bool approach) {
			if(shouldResetStats)
				orig(self, approach);
		}

		private static void AddExtraModes(OuiChapterPanel self) {
			// check map meta for extra sides or side overrides
			AltSidesHelperMeta meta = GetMetaForAreaData(self.Data);
			if(meta?.Sides != null) {
				Logger.Log("AltSidesHelper", $"Customising panel UI for \"{self.Data.SID}\".");
				bool[] unlockedSides = new bool[meta.Sides.Count()];
				int siblings = ((IList)modesField.GetValue(self)).Count;
				int oldModes = siblings;
				bool bsidesunlocked = !self.Data.Interlude_Safe && self.Data.HasMode(AreaMode.BSide) && (self.DisplayedStats.Cassette || ((SaveData.Instance.DebugMode || SaveData.Instance.CheatMode) && self.DisplayedStats.Cassette == self.RealStats.Cassette));
				bool csidesunlocked = !self.Data.Interlude_Safe && self.Data.HasMode(AreaMode.CSide) && SaveData.Instance.UnlockedModes >= 3 && Celeste.Celeste.PlayMode != Celeste.Celeste.PlayModes.Event;
				// find the new total number of unlocked modes
				int unlockedModeCount = 0;
				// if this map has a C-Side, this is whether they have C-sides unlocked. else, if this map has a B-Sides, its whether they have a cassette. else, true.
				bool prevUnlocked = self.Data.HasMode(AreaMode.CSide) ? csidesunlocked : self.Data.HasMode(AreaMode.BSide) ? bsidesunlocked : true;
				// if this map has a C-Side, this is whether they've beaten it; else, if this map has a B-Side, its whether they've completed it; else, its whether they've completed the level.
				bool prevCompleted = self.Data.HasMode(AreaMode.CSide) ? SaveData.Instance.GetAreaStatsFor(self.Data.ToKey()).Modes[(int)AreaMode.CSide].Completed : self.Data.HasMode(AreaMode.BSide) ? SaveData.Instance.GetAreaStatsFor(self.Data.ToKey()).Modes[(int)AreaMode.BSide].Completed : SaveData.Instance.GetAreaStatsFor(self.Data.ToKey()).Modes[(int)AreaMode.Normal].Completed;
				for(int i1 = 0; i1 < meta.Sides.Length; i1++) {
					AltSidesHelperMode mode = meta.Sides[i1];
					if(!mode.OverrideVanillaSideData) {
						if(mode.UnlockMode != null && AltSidesSaveData.UnlockedAltSideIDs != null && ((mode.UnlockMode.Equals("consecutive") && prevCompleted) || (mode.UnlockMode.Equals("with_previous") && prevUnlocked) || (mode.UnlockMode.Equals("triggered") && AltSidesSaveData.UnlockedAltSideIDs.Contains(mode.Map)) || (mode.UnlockMode.Equals("c_sides_unlocked") && csidesunlocked) || mode.UnlockMode.Equals("always")) || (SaveData.Instance != null && (SaveData.Instance.DebugMode || SaveData.Instance.CheatMode))) {
							unlockedModeCount++;
							siblings++;
							prevUnlocked = true;
							prevCompleted = SaveData.Instance.GetAreaStatsFor(AreaData.Get(mode.Map).ToKey()).Modes[(int)AreaMode.Normal].Completed;
							unlockedSides[i1] = true;
						} else {
							prevUnlocked = prevCompleted = false;
							unlockedSides[i1] = false;
						}
					} else
						unlockedSides[i1] = true;
				}
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
				int newSides = 0;
				for(int i = 0; i < meta.Sides.Length /*&& newSides < unlockedModes*/; i++) {
					AltSidesHelperMode mode = meta.Sides[i];
					// only add if its unlocked
					if(unlockedSides[i]) {
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
							newSides++;
							Logger.Log("AltSidesHelper", $"Added new side for \"{self.Data.SID}\".");
						} else {
							// find the a-side and modify it
							DynamicData data = new DynamicData(((IList)modesField.GetValue(self))[0]);
							data.Set("Label", Dialog.Clean(mode.Label));
							data.Set("Icon", GFX.Gui[mode.Icon]);
							Logger.Log("AltSidesHelper", $"Modifying A-Side data for \"{self.Data.SID}\".");
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

		private delegate int orig_LevelSetStats_get_MaxArea(LevelSetStats self);
		private static int OnLevelSetStatsGetMaxArea(orig_LevelSetStats_get_MaxArea orig, LevelSetStats self) {
			int prevArea = orig(self);
			// take off any alt-sides
			return prevArea - AreaData.Areas.Count((AreaData area) => area.GetLevelSet() == self.Name && !string.IsNullOrEmpty(GetMetaForAreaData(area)?.AltSideData.For));
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

		private delegate bool orig_Session_get_FullClear(Session self);
		private static bool OnSessionGetFullClear(orig_Session_get_FullClear orig, Session self) {
			var prev = orig(self);
			var meta = GetModeMetaForAltSide(AreaData.Get(self));
			if(meta != null && meta.CanFullClear && (!meta.CassetteNeededForFullClear || self.Cassette) && (!meta.HeartNeededForFullClear || self.HeartGem) && (self.Strawberries.Count >= self.MapData.DetectedStrawberries)) 
				return true;
			return prev;
		}

		private void PostAreaLoad(On.Celeste.AreaData.orig_Load orig) {
			orig();
			var heartTextures = new HashSet<string>();
			int altsides = 0;
			foreach(var map in AreaData.Areas) {
				// Load "mapdir/mapname.altsideshelper.meta.yaml" as a AltSidesHelperMeta
				AltSidesHelperMeta meta;
				if(Everest.Content.TryGet("Maps/" + map.Mode[0].Path + ".altsideshelper.meta", out ModAsset metadata) && metadata.TryDeserialize(out meta)) {
					foreach(var mode in meta.Sides) {
						mode.ApplyPreset();
						altsides++;
						heartTextures.Add(mode.ChapterPanelHeartIcon);
						if(mode.OverrideVanillaSideData) {
							Logger.Log(LogLevel.Info, "AltSidesHelper", $"Will customise A-Side for \"{map.SID}\".");
						}
					}
					// Attach the meta to the AreaData
					AltSidesMetadata[map] = meta;
					if(meta.AltSideData.IsAltSide) {
						var aside = AreaData.Get(meta.AltSideData.For);
						if(meta.AltSideData.CopyEndScreenData)
							map.Meta.CompleteScreen = aside.Meta.CompleteScreen;
						map.Meta.Mountain = aside.Meta.Mountain;
						map.MountainCursor = aside.MountainCursor;
						map.MountainCursorScale = aside.MountainCursorScale;
						map.MountainIdle = aside.MountainIdle;
						map.MountainSelect = aside.MountainSelect;
						map.MountainState = aside.MountainState;
						map.MountainZoom = aside.MountainZoom;
						map.TitleAccentColor = aside.TitleAccentColor;
						map.TitleBaseColor = aside.TitleBaseColor;
						map.TitleTextColor = aside.TitleTextColor;
						if(meta.AltSideData.CopyTitle)
							map.Name = aside.Name;
					}
				}
			}

			Logger.Log(LogLevel.Info, "AltSidesHelper", $"Loaded {altsides} alt-sides!");

			SpriteBank crystalHeartSwaps = new SpriteBank(GFX.Gui, "Graphics/AltSidesHelper/Empty.xml");

			// TODO: allow using XMLs too - load them and copy them in
			Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

			foreach(var heart in heartTextures) {
				// our sprite ID will be "<heart sprite path keyified>"
				// we're talking along the lines of "collectables/heartgem/0/spin"
				// use the last part of the name as the loop paths, and the rest as element path

				var parts = heart.Split('/');
				var loopPath = parts[parts.Length - 1];
				string elemPath = heart.Substring(0, heart.Length - loopPath.Length);

				var sprite = new Sprite(GFX.Gui, elemPath);
				sprite.AddLoop("idle", loopPath, 0, new int[] { 0 });
				sprite.AddLoop("spin", loopPath, 0.08f, new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
				sprite.AddLoop("fastspin", loopPath, 0.08f, new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
				sprite.Play("idle");
				sprite.CenterOrigin();
				sprites.Add(heart.DialogKeyify(), sprite);
			}

			int hearts = 0;
			foreach(var kvp in sprites) {
				hearts++;
				crystalHeartSwaps.SpriteData[kvp.Key] = new SpriteData(GFX.Gui) {
					Sprite = kvp.Value
				};
			}

			HeartSpriteBank = crystalHeartSwaps;
			Logger.Log(LogLevel.Info, "AltSidesHelper", $"Loaded {hearts} crystal heart UI textures.");
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

		private IEnumerator AddAltSideRemixTitle(On.Celeste.LevelEnter.orig_Routine orig, LevelEnter self){
			var data = new DynData<LevelEnter>(self);
			var session = data.Get<Session>("session");

			if (session.StartedFromBeginning && !data.Get<bool>("fromSaveData") && (GetModeMetaForAltSide(AreaData.Get(session.Area))?.ShowBSideRemixIntro ?? false)){
				AltSideTitle title = new AltSideTitle(session);
				self.Add(title);
				Audio.Play("event:/ui/main/bside_intro_text");
				yield return title.EaseIn();
				yield return 0.25f;
				yield return title.EaseOut();
				yield return 0.25f;
			}

			yield return new SwapImmediately(orig(self));
		}

		public static AltSidesHelperMeta GetMetaForAreaData(AreaData data){
			if(data == null)
				return null;
			if(!AltSidesMetadata.ContainsKey(data))
				return null;
			return AltSidesMetadata[data];//new DynData<AreaData>(data).Get<AltSidesHelperMeta>("AltSidesHelperMeta");
		}

		public static AltSidesHelperMode GetModeMetaForAltSide(AreaData data) {
			if(data == null)
				return null;
			AltSidesHelperMeta parentHelperMeta = GetMetaForAreaData(AreaData.Get(GetMetaForAreaData(AreaData.Get(data.SID))?.AltSideData?.For));
			if(parentHelperMeta != null)
				foreach(var mode in parentHelperMeta.Sides)
					if(mode.Map.Equals(data.SID))
						return mode;
			// check for a-side overrides too
			AltSidesHelperMeta helperMeta = GetMetaForAreaData(data);
			if(helperMeta != null)
				foreach(var mode in helperMeta.Sides)
					if(mode.OverrideVanillaSideData)
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

		public bool CopyEndScreenData {
			get;
			set;
		} = true;

		public bool CopyTitle {
			get;
			set;
		} = true;
	}

	public class AltSidesHelperMode {

		public string Map {
			get;
			set;
		} = "";

		public string Preset {
			get;
			set;
		} = "none";

		public string UnlockMode {
			get;
			set;
		} = "consecutive";

		public bool OverrideHeartTextures {
			get;
			set;
		} = true;

		public bool? ShowBerriesAsGolden {
			get;
			set;
		} = null;

		// Dialog key
		public string Label {
			get;
			set;
		} = "";

		public string EndScreenTitle {
			get;
			set;
		} = "";

		public string EndScreenClearTitle {
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

		public string JournalCassetteIcon {
			get;
			set;
		} = "";

		public bool AddCassetteIcon {
			get;
			set;
		} = false;

		// Hex colour code
		public string HeartColour {
			get;
			set;
		} = "";

		public bool? ShowHeartPoem {
			get;
			set;
		} = null;

		public bool? ShowBSideRemixIntro {
			get;
			set;
		} = null;

		// For overriding vanilla side data
		public bool OverrideVanillaSideData {
			get;
			set;
		} = false;

		// Full-clear info
		public bool CanFullClear {
			get;
			set;
		} = false;

		public bool CassetteNeededForFullClear {
			get;
			set;
		} = true;

		public bool HeartNeededForFullClear {
			get;
			set;
		} = true;

		private static readonly AltSidesHelperMode A_SIDE_PRESET = new AltSidesHelperMode()
		{
			Label = "OVERWORLD_NORMAL",
			Icon = "menu/play",
			DeathsIcon = "collectables/skullBlue",
			ChapterPanelHeartIcon = "collectables/heartgem/0/spin",
			InWorldHeartIcon = "collectables/heartGem/0/",
			JournalHeartIcon = "heartgem0",
			JournalCassetteIcon = "cassette",
			HeartColour = "8cc7fa",
			EndScreenTitle = "AREACOMPLETE_NORMAL",
			EndScreenClearTitle = "AREACOMPLETE_NORMAL_FULLCLEAR",
			ShowHeartPoem = true,
			ShowBSideRemixIntro = false
		};

		private static readonly AltSidesHelperMode B_SIDE_PRESET = new AltSidesHelperMode()
		{
			Label = "OVERWORLD_REMIX",
			Icon = "menu/remix",
			DeathsIcon = "collectables/skullRed",
			ChapterPanelHeartIcon = "collectables/heartgem/1/spin",
			InWorldHeartIcon = "collectables/heartGem/1/",
			JournalHeartIcon = "heartgem1",
			JournalCassetteIcon = "cassette",
			HeartColour = "ff668a",
			EndScreenTitle = "AREACOMPLETE_BSIDE",
			EndScreenClearTitle = "leppa_AltSidesHelper_areacomplete_fullclear_bside",
			ShowHeartPoem = true,
			ShowBSideRemixIntro = true
		};

		private static readonly AltSidesHelperMode C_SIDE_PRESET = new AltSidesHelperMode()
		{
			Label = "OVERWORLD_REMIX2",
			Icon = "menu/rmx2",
			DeathsIcon = "collectables/skullGold",
			ChapterPanelHeartIcon = "collectables/heartgem/2/spin",
			InWorldHeartIcon = "collectables/heartGem/2/",
			JournalHeartIcon = "heartgem2",
			JournalCassetteIcon = "cassette",
			HeartColour = "fffc24",
			EndScreenTitle = "AREACOMPLETE_CSIDE",
			EndScreenClearTitle = "leppa_AltSidesHelper_areacomplete_fullclear_cside",
			ShowHeartPoem = false,
			ShowBSideRemixIntro = false
		};

		private static readonly AltSidesHelperMode D_SIDE_PRESET = new AltSidesHelperMode()
		{
			// TODO: Missing icons
			Label = "leppa_AltSidesHelper_overworld_remix3",
			Icon = "menu/leppa/AltSidesHelper/rmx3",
			DeathsIcon = "collectables/skullGold",
			ChapterPanelHeartIcon = "collectables/leppa/AltSidesHelper/heartgem/dside",
			InWorldHeartIcon = "collectables/heartGem/3/",
			JournalHeartIcon = "leppa/AltSidesHelper/heartgemD",
			JournalCassetteIcon = "leppa/AltSidesHelper/cassetteD",
			HeartColour = "ffffff",
			EndScreenTitle = "leppa_AltSidesHelper_areacomplete_dside",
			EndScreenClearTitle = "leppa_AltSidesHelper_areacomplete_fullclear_dside",
			ShowHeartPoem = true,
			ShowBSideRemixIntro = false
		};

		// TODO: allow defining custom presets

		public void ApplyPreset() {
			if(Preset.Equals("a-side")){
				CopySettings(A_SIDE_PRESET);
			} else if(Preset.Equals("b-side")) {
				CopySettings(B_SIDE_PRESET);
			} else if(Preset.Equals("c-side")) {
				CopySettings(C_SIDE_PRESET);
			} else if(Preset.Equals("d-side")) {
				CopySettings(D_SIDE_PRESET);
			}
		}

		public void CopySettings(AltSidesHelperMode from) {
			if (string.IsNullOrEmpty(Label))
				Label = from.Label;
			if (string.IsNullOrEmpty(Icon))
				Icon = from.Icon;
			if (string.IsNullOrEmpty(DeathsIcon))
				DeathsIcon = from.DeathsIcon;
			if (string.IsNullOrEmpty(ChapterPanelHeartIcon))
				ChapterPanelHeartIcon = from.ChapterPanelHeartIcon;
			if (string.IsNullOrEmpty(InWorldHeartIcon))
				InWorldHeartIcon = from.InWorldHeartIcon;
			if (string.IsNullOrEmpty(JournalHeartIcon))
				JournalHeartIcon = from.JournalHeartIcon;
			if(string.IsNullOrEmpty(JournalCassetteIcon))
				JournalCassetteIcon = from.JournalCassetteIcon;
			if (string.IsNullOrEmpty(HeartColour))
				HeartColour = from.HeartColour;
			if (string.IsNullOrEmpty(EndScreenTitle))
				EndScreenTitle = from.EndScreenTitle;
			if (string.IsNullOrEmpty(EndScreenClearTitle))
				EndScreenClearTitle = from.EndScreenClearTitle;
			if (ShowHeartPoem == null)
				ShowHeartPoem = from.ShowHeartPoem;
			if (ShowBSideRemixIntro == null)
				ShowBSideRemixIntro = from.ShowBSideRemixIntro;
		}

		public AltSidesHelperMode Copy() {
			var th = this;
			return new AltSidesHelperMode {
				Label = th.Label,
				Icon = th.Icon,
				DeathsIcon = th.DeathsIcon,
				ChapterPanelHeartIcon = th.ChapterPanelHeartIcon,
				InWorldHeartIcon = th.InWorldHeartIcon,
				JournalHeartIcon = th.JournalHeartIcon,
				JournalCassetteIcon = th.JournalCassetteIcon,
				HeartColour = th.HeartColour,
				EndScreenTitle = th.EndScreenTitle,
				EndScreenClearTitle = th.EndScreenClearTitle,
				ShowHeartPoem = th.ShowHeartPoem,
				ShowBSideRemixIntro = th.ShowBSideRemixIntro
			};
		}
	}
}
