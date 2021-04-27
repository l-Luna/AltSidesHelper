using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace AltSidesHelper.Triggers {

	[CustomEntity("AltSidesHelper/AltSideUnlockTrigger")]
	class AltSideUnlockTrigger : Trigger{

		private string altSideToUnlock;

		public AltSideUnlockTrigger(EntityData data, Vector2 offset) : base(data, offset) {
			altSideToUnlock = data.Attr("altSideToUnlock");
		}

		public override void OnEnter(Player player) {
			if(!string.IsNullOrEmpty(altSideToUnlock))
				AltSidesHelperModule.AltSidesSaveData.UnlockedAltSideIDs.Add(altSideToUnlock);
		}
	}
}
