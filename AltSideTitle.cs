using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;

namespace AltSidesHelper{

	class AltSideTitle : Entity {

		protected string[] text = new string[0];
		protected float[] fade;
		protected float[] offsets;
		protected float offset;

		public AltSideTitle(Session session) : base(){
			AreaData areaData = AreaData.Get(session);
			string name = areaData.SID.DialogKeyify();
			if (Dialog.Has(name + "_altsides_remix_intro")) {
				// look for a list: "{name}_altsides_remix_intro"
				text = Dialog.Get(name + "_altsides_remix_intro").Split(new string[] { "{break}" }, System.StringSplitOptions.RemoveEmptyEntries);
			}else{
				// use the Everest format
				// level, artist, album
				text = new string[] {
					Dialog.Get(areaData.Name) + " " + Dialog.Get(name + "_remix"),
					Dialog.Get("remix_by") + " " + Dialog.Get(name + "_remix_artist"),
					Dialog.Has(name + "_remix_album") ? Dialog.Get(name + "_remix_album") : Dialog.Get("remix_album")
				};
			}
			fade = new float[text.Length];
			offsets = new float[text.Length];
			Tag = Tags.HUD;
			Visible = true;
		}

		public IEnumerator EaseIn() {
			for(int i = 0; i < text.Length; i++) {
				Add(new Coroutine(FadeTo(i, 1f, 1f)));
				yield return .2f;
			}
			yield return 1.6f;
		}

		public IEnumerator EaseOut() {
			for(int i = 0; i < text.Length; i++) {
				Add(new Coroutine(FadeTo(i, 0f, 1f)));
				yield return .2f;
			}
			yield return 1.6f;
		}

		public override void Update() {
			base.Update();
			offset += Engine.DeltaTime * 32f;
		}

		public override void Render() {
			Vector2 value = new Vector2(60f + offset, 800f - MathHelper.Max(text.Length * 60 - 180, 0));
			for(int i = 0; i < text.Length; i++) {
				string item = text[i];
				ActiveFont.Draw(item, value + new Vector2(offsets[i], 60f * i), Color.White * fade[i]);
				//Logger.Log("AltSidesHelper", $"Rendering \"{item}\" (no. {i}) with {fade[i]} colour and {offsets[i]} offset at {value + new Vector2(offsets[i], 60f * i)}");
			}
		}

		private IEnumerator FadeTo(int index, float target, float duration) {
			while((fade[index] = Calc.Approach(fade[index], target, Engine.DeltaTime / duration)) != target) {
				if(target == 0f) {
					offsets[index] = Ease.CubeIn(1f - fade[index]) * 32f;
				} else {
					offsets[index] = (0f - Ease.CubeIn(1f - fade[index])) * 32f;
				}

				yield return null;
			}
		}
	}
}
