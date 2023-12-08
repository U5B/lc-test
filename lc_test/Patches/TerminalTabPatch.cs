
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
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

	public class ScenePatch
	{
		[HarmonyPatch(typeof(Terminal))]
		[HarmonyPrefix]
		[HarmonyPatch("QuitTerminal")]
		public static void DoneInteractingWithTerminal()
		{
			GameNetworkManager.Instance.localPlayerController.inSpecialInteractAnimation = false;
		}
		public static void Awake()
		{
			SceneManager.activeSceneChanged += ActiveSceneChanged;
		}
		private static void ActiveSceneChanged(Scene arg0, Scene arg1)
		{
			Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
			if (terminal == null)
			{
				return;
			}
			terminal.terminalUIScreen.renderMode = RenderMode.ScreenSpaceOverlay;
			terminal.terminalUIScreen.scaleFactor += 1.35f;
		}
	}
}