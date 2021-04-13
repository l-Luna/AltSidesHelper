using Celeste.Mod;
using Celeste;
using System.Reflection;
using System;
using System.Collections;
using MonoMod.Utils;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace DSidesHelper {
	public class DSidesModule : EverestModule {

		public static DSidesModule Instance;
		public static int MaxSides = 4; // TODO: add proper API for setting max sides

		private static readonly Type t_OuiChapterPanelOption = typeof(OuiChapterPanel)
			.GetNestedType("Option", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

		public DSidesModule() {
			Instance = this;
		}

		public override void Load() {
			// Modify AreaData parsing
			// TODO: less replacement
			DynData<AreaData> areaData = new DynData<AreaData>();
			areaData.Set("ParseNameRegex", new Regex("^(?:(?<order>\\d+)(?<side>[ABCHXD]?)\\-)?(?<name>.+?)(?:\\-(?<sideAlt>[ABCHXD]?))?$", RegexOptions.Compiled));
			areaData.Get<Dictionary<string, AreaMode>>("ParseNameModes").Add("D", (AreaMode)3);

			On.Celeste.OuiChapterPanel.Reset += OnChapterPanelReset;
			On.Celeste.AreaStats.ctor += OnAreaStatsCtor;
			On.Celeste.AreaStats.ctor_int += OnAreaStatsCtorInt;
			IL.Celeste.AreaData.Load += ModifyAreaDataLoad;
			On.Celeste.AreaData.Load += OnAreaDataLoad;
		}

		public override void LoadContent(bool firstLoad) {
			
		}

		public override void Unload() {
			On.Celeste.OuiChapterPanel.Reset -= OnChapterPanelReset;
			On.Celeste.AreaStats.ctor -= OnAreaStatsCtor;
			On.Celeste.AreaStats.ctor_int -= OnAreaStatsCtorInt;
			IL.Celeste.AreaData.Load -= ModifyAreaDataLoad;
			On.Celeste.AreaData.Load -= OnAreaDataLoad;
		}

		private void OnChapterPanelReset(On.Celeste.OuiChapterPanel.orig_Reset orig, OuiChapterPanel self) {
			orig(self);
			Logger.Log("DSidesHelper", self.Data.SID + " has " + self.Data.Mode.Length.ToString() + " sides");
			if(self.Data.Mode.Length >= 4) {
				Logger.Log("DSidesHelper", "Side: " + (self.Data.Mode[3]?.ToString() ?? "null"));
				Logger.Log("DSidesHelper", "Side path: " + (self.Data.Mode[3]?.Path ?? "null"));
			}
			if(self.Data.HasMode((AreaMode)3)){
				var modesField = typeof(OuiChapterPanel).GetField("modes", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
				((IList)modesField.GetValue(self)).Add(
					DynamicData.New(t_OuiChapterPanelOption)(new {
						Label = Dialog.Clean("leppa_DSidesHelper_overworld_remix3"),
						Icon = GFX.Gui["menu/leppa_DSidesHelper/rmx3"],
						ID = "D"
					})
				);
			}
		}

		private void OnAreaStatsCtor(On.Celeste.AreaStats.orig_ctor orig, AreaStats self) {
			orig(self);
			int length = MaxSides;
			self.Modes = new AreaModeStats[length];
			for(int i = 0; i < self.Modes.Length; i++) {
				self.Modes[i] = new AreaModeStats();
			}
		}

		private void OnAreaStatsCtorInt(On.Celeste.AreaStats.orig_ctor_int orig, AreaStats self, int k) {
			orig(self, k);
			int length = MaxSides;
			self.Modes = new AreaModeStats[length];
			for(int i = 0; i < self.Modes.Length; i++) {
				self.Modes[i] = new AreaModeStats();
			}
		}

		private void ModifyAreaDataLoad(ILContext ctx) {
			/*
			 * Want to modify: 
			 * 
			   mapMeta.Modes = (mapMeta.Modes ?? new MapMetaModeProperties[3]);
					if(mapMeta.Modes.Length < 3) {
						MapMetaModeProperties[] array = new MapMetaModeProperties[3];
						for(int k = 0; k < mapMeta.Modes.Length; k++) {
							array[k] = mapMeta.Modes[k];
						}

						mapMeta.Modes = array;
					}

					if(areaData.Mode.Length < 3) {
						ModeProperties[] array2 = new ModeProperties[3];
						for(int l = 0; l < areaData.Mode.Length; l++) {
							array2[l] = areaData.Mode[l];
						}

						areaData.Mode = array2;
					}
			 * and replace 3 with MaxSides
			 * these are the only constant 3s actually, so lets replace all of them
			 * what could go wrong?
			 */
			// TODO: do something more sane...
			// use MaxSides
			foreach(var instr in ctx.Instrs) {
				if(instr.OpCode == OpCodes.Ldc_I4_3) {
					instr.OpCode = OpCodes.Ldc_I4_4;
				}
			}
		}

		private void OnAreaDataLoad(On.Celeste.AreaData.orig_Load orig) {
			orig();
			// allow C/D side poems with
			/*
			  if(areaData4.Mode.Length > 1 && areaData4.Mode[1] != null && areaData4.Mode[1].PoemID == null) {
					areaData4.Mode[1].PoemID = areaData4.GetSID().DialogKeyify() + "_B";
				}
			 */
			// after checking map meta

			int dsides = 0;
			foreach(var map in AreaData.Areas) {
				if(map.Mode.Length >= 4)
					dsides++;
				Logger.Log("DSidesHelper", $"{map.SID} - {map.Mode.Length} sides");
			}
				
			Logger.Log("DSidesHelper", $"Found {dsides} maps with D-Sides, out of {AreaData.Areas.Count}.");
		}
	}
}
