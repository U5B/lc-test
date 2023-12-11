using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using BepInEx.Configuration;
using HarmonyLib;
using lc_test.Patches;

namespace lc_test
{
	[BepInPlugin(GUID, NAME, VERSION)]
	public class Plugin : BaseUnityPlugin
	{

		public const string GUID = "net.usbwire.usb.lc_test";
		public const string NAME = "lc_test";
		public const string VERSION = "1.1.0";

		internal static ManualLogSource mls;
		private void Awake()
		{
			mls = Logger;
			// Plugin startup logic
			mls.LogInfo($"Plugin {GUID} is loaded!");
			Harmony harmony = new Harmony(GUID);
			harmony.PatchAll(Assembly.GetExecutingAssembly());

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