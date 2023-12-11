
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace lc_test.Patches
{
	[HarmonyPatch(typeof(HUDManager))]
	public class HUDManagerPatch
	{
		[HarmonyPrefix]
		[HarmonyPatch("SubmitChat_performed")]
		private static void HandleChatCommand(HUDManager __instance)
		{
			if (__instance == null)
			{
				return;
			}

			string command = __instance.chatTextField.text[..];
			if (!command.StartsWith("!"))
			{
				return;
			}

			__instance.chatTextField.text = "";

			if (command == "!terminal")
			{
				if (!GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom)
				{
					return;
				}
				// Copied from GameMaster mod v2.5
				// https://github.com/catmcfish/LethalCompanyGameMaster/releases/tag/2.5
				Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
				if (terminal == null)
				{
					return;
				}
				terminal.BeginUsingTerminal();
				HUDManager.Instance.ChangeControlTip(0, string.Empty, true);
				GameNetworkManager.Instance.localPlayerController.inSpecialInteractAnimation = true;
			}
		}
	}
}