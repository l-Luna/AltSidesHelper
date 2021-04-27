using Celeste.Mod;
using System.Collections.Generic;

namespace AltSidesHelper {

	public class AltSidesHelperSaveData : EverestModuleSaveData {

		// Store alt-sides that have been unlocked by trigger or cassette
		public HashSet<string> UnlockedAltSideIDs = new HashSet<string>();
	}
}