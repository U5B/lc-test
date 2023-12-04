using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Unity.Netcode;

namespace lc_test.Patches
{
	// Token: 0x02000003 RID: 3
	[HarmonyPatch(typeof(Terminal))]
	internal class TerminalPatch
	{
		[HarmonyPrefix]
		[HarmonyPatch("Update")]
		private static bool muteKeyboardSound(ref Terminal __instance) {
			__instance.timeSinceLastKeyboardPress = 0f;
			return true;
		}

		[HarmonyPostfix]
		[HarmonyPatch("ParsePlayerSentence")]
		private static void HandleCommands(ref Terminal __instance, ref TerminalNode __result)
		{
			string text = __instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded).ToLower();
			if (text.StartsWith("tp"))
			{
				// Thanks to TerminalTP for this code to find ShipTeleporter & push the button
				// https://thunderstore.io/c/lethal-company/p/malco/Terminal_TP/
				ShipTeleporter[] shipTeleporters = UnityEngine.Object.FindObjectsOfType<ShipTeleporter>();
				ShipTeleporter shipTeleporter = null;
				foreach (ShipTeleporter shipTeleporter2 in shipTeleporters)
				{
					if (!shipTeleporter2.isInverseTeleporter)
					{
						shipTeleporter = shipTeleporter2;
						break;
					}
				}
				if (shipTeleporter != null)
				{
					__result = new TerminalNode
					{
						displayText = "Initializing Teleporter for '" + StartOfRound.Instance.mapScreen.targetedPlayer.playerUsername + "'\n\n",
						clearPreviousText = true,
						playSyncedClip = -1
					};
					shipTeleporter.PressTeleportButtonOnLocalClient();
				}
				return;
			}

			string[] parameters = text.Split(" ");
			// Open/Close doors
			if (parameters[0] == "d" || parameters[0] == "door")
			{
				HangarShipDoor hangarShipDoor = Object.FindObjectOfType<HangarShipDoor>();
				if (hangarShipDoor == null || !hangarShipDoor.buttonsEnabled)
				{
					__result = new TerminalNode
					{
						displayText = "Hangar Doors are disabled!\n\n",
						clearPreviousText = true
					};
					return;
				}
				bool openDoor = StartOfRound.Instance.hangarDoorsClosed;
				if (parameters.Length > 1)
				{
					if (parameters[1] == "c" || parameters[1].StartsWith("close"))
					{
						openDoor = false;
					}
					else if (parameters[1] == "o" || parameters[1].StartsWith("open"))
					{
						openDoor = true;
					}
				}
				__result = ToggleHangarDoor(openDoor);
			}
			else if (parameters[0] == "dc") // shorthand for closing doors quickly
			{
				__result = ToggleHangarDoor(false);
			}
			// Better Switch parameters
			else if (parameters[0] == "s" || parameters[0] == "sw")
			{
				if (parameters.Length > 1)
				{
					var output = FindRadarNumFromName(parameters[1]);
					int num = output.Item1;
					if (num != -1)
					{
						__result = new TerminalNode
						{
							displayText = "Switching to radar target: '" + output.Item2 + "'\n\n",
							clearPreviousText = true,
							playSyncedClip = -1
						};
						StartOfRound.Instance.mapScreen.SwitchRadarTargetAndSync(num);
					}
					return;
				}
				__result = __instance.terminalNodes.specialNodes[20];
				__result.playSyncedClip = -1;
				__result.playClip = null;
				StartOfRound.Instance.mapScreen.SwitchRadarTargetForward(true);
			}
			else if (parameters[0] == "sb")
			{
				__result = __instance.terminalNodes.specialNodes[20];
				__result.playSyncedClip = -1;
				__result.playClip = null;
				StartOfRound.Instance.mapScreen.SwitchRadarTargetAndSync(GetRadarTargetIndexMinusOne());
			}
		}

		private static int GetRadarTargetIndexMinusOne() {
				int index = StartOfRound.Instance.mapScreen.targetTransformIndex;
			  if (index - 1 < 0)
        {
          return StartOfRound.Instance.mapScreen.radarTargets.Count - 1;
        }
        return index - 1;
		}

		private static TerminalNode ToggleHangarDoor(bool openDoor) {
			GameObject gameObject = GameObject.Find(openDoor ? "StartButton" : "StopButton");
			if (gameObject != null)
			{
				AnimatedObjectTrigger componentInChildren = gameObject.GetComponentInChildren<AnimatedObjectTrigger>();
				if (componentInChildren != null)
				{
					componentInChildren.TriggerAnimationNonPlayer(false, false, false);
					return new TerminalNode
					{
						displayText = (openDoor ? "Opened" : "Closed") + " Hangar Doors\n\n",
						clearPreviousText = true,
						playSyncedClip = -1
					};
				}
			}
			return null;
		}

		private static (int, string) FindRadarNumFromName(string input) {
			string name = input.ToLower();
			string outputName = "";
			List<string> nameList = new List<string>();
			int num = -1;
			for (int j = 0; j < StartOfRound.Instance.mapScreen.radarTargets.Count; j++)
			{
				nameList.Add(StartOfRound.Instance.mapScreen.radarTargets[j].name);
			}
			for (int k = 0; k < nameList.Count; k++)
			{
				outputName = nameList[k].ToLower();
				if (outputName.StartsWith(name))
				{
					num = k;
					break;
				}
			}
			if (num == -1 && name.Length > 1)
			{
				for (int l = 0; l < nameList.Count; l++)
				{
					outputName = nameList[l].ToLower();
					if (outputName.Contains(name))
					{
						num = l;
						break;
					}
				}
			}
			return (num, outputName);
		}
	}
}
