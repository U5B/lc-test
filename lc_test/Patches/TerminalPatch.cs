using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using GameNetcodeStuff;

namespace lc_test.Patches
{
	// Token: 0x02000003 RID: 3
	[HarmonyPatch(typeof(Terminal))]
	internal class TerminalPatch
	{
		[HarmonyPrefix]
		[HarmonyPatch("Update")]
		private static bool MuteKeyboardSounds(ref Terminal __instance)
		{
			__instance.timeSinceLastKeyboardPress = 0f;
			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch("PlayBroadcastCodeEffect")]
		private static bool MuteCodeBroadcastSounds(ref Terminal __instance)
		{
			__instance.codeBroadcastAnimator.SetTrigger("display");
			// __instance.terminalAudio.PlayOneShot(__instance.codeBroadcastSFX, 1f);
			return false;
		}

		[HarmonyPostfix]
		[HarmonyPatch("ParsePlayerSentence")]
		private static void HandleTerminalCommands(ref Terminal __instance, ref TerminalNode __result)
		{
			if (__result == null || (__result.name != "ParserError1" && __result.name != "ParserError2" && __result.name != "ParserError3"))
			{
				return;
			}

			string text = __instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded).ToLower();
			if (text.StartsWith("tp"))
			{
				__result = TriggerTeleporter();
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
						playSyncedClip = -1,
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
				return;
			}
			else if (parameters[0] == "dc") // shorthand for closing doors quickly
			{
				__result = ToggleHangarDoor(false);
				return;
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
							playSyncedClip = -1
						};
						StartOfRound.Instance.mapScreen.SwitchRadarTargetAndSync(num);
					}
					return;
				}
				__result = __instance.terminalNodes.specialNodes[20];
				__result.playSyncedClip = -1;
				StartOfRound.Instance.mapScreen.SwitchRadarTargetForward(true);
				return;
			}
			else if (parameters[0] == "sb" || parameters[0] == "swb")
			{
				__result = __instance.terminalNodes.specialNodes[20];
				__result.playSyncedClip = -1;
				// be smart about switching backwards incase of null players
				int index = SwitchRadarTargetBack();
				if (index != -1)
				{
					StartOfRound.Instance.mapScreen.SwitchRadarTargetAndSync(index);
				}
				return;
			}
			else if (parameters[0] == "cam")
			{
				if (GameObject.Find("Environment/HangarShip/Cameras/ShipCamera") != null)
				{
					// Camera code from https://github.com/darmuh/TerminalStuff/blob/811abf20080b2881d46ecc9cf555f21ea61e7eac/DarmuhsTerminalCommands/LeaveTerminal.cs#L103
					Texture cameraTexture = GameObject.Find("Environment/HangarShip/ShipModels2b/MonitorWall/Cube.001").GetComponent<MeshRenderer>().materials[2].mainTexture;
					__result = new TerminalNode
					{
						displayTexture = cameraTexture,
						displayText = "Toggling ship camera\n\n",
						persistentImage = true,
						loadImageSlowly = false,
						clearPreviousText = true,
						playSyncedClip = -1
					};
				}
				return;
			}
			else if (parameters[0] == "mon")
			{
				// "view monitor" - TerminalNode = "ViewInsideShipCam 1"
				__result = __instance.terminalNodes.allKeywords[19].compatibleNouns[12].result;
				__result.loadImageSlowly = false;
				__result.playSyncedClip = -1;
				return;
			}
			else if (parameters[0] == "lights")
			{
				StartOfRound.Instance.shipRoomLights.ToggleShipLights();
				__result = new TerminalNode
				{
					displayText = "Toggling lights\n\n",
					clearPreviousText = true,
					playSyncedClip = -1
				};
				return;
			}
			else if (parameters[0] == "dt" || parameters[0] == "dm")
			{
				DisableTurretsAndMines();
				__instance.PlayBroadcastCodeEffect();
				__result = new TerminalNode
				{
					displayText = "",
					playSyncedClip = -1
				};
				return;
			}
			else if (parameters[0] == "exit")
			{
				__result = new TerminalNode
				{
					displayText = "",
					clearPreviousText = true,
					playSyncedClip = -1
				};
				__instance.QuitTerminal();
				return;
			}
		}

		private static TerminalNode TriggerTeleporter()
		{
			// Thanks to TerminalTP for this code to find ShipTeleporter & push the button
			// https://thunderstore.io/c/lethal-company/p/malco/Terminal_TP/
			ShipTeleporter[] shipTeleporters = UnityEngine.Object.FindObjectsOfType<ShipTeleporter>();
			ShipTeleporter shipTeleporter = null;
			foreach (ShipTeleporter teleporter in shipTeleporters)
			{
				if (!teleporter.isInverseTeleporter)
				{
					shipTeleporter = teleporter;
					break;
				}
			}
			if (shipTeleporter == null) {
				return new TerminalNode
				{
					displayText = "No teleporter found!\n\n",
					playSyncedClip = -1
				};
			}
			if (shipTeleporter.buttonTrigger.interactable == false && shipTeleporter.cooldownTime > 0f) {
				return new TerminalNode
				{
					displayText = "Teleporter is on cooldown for " + (int)shipTeleporter.cooldownTime  + " seconds",
					playSyncedClip = -1
				};
			}
			shipTeleporter.PressTeleportButtonOnLocalClient();
			return new TerminalNode
			{
				displayText = "Initializing Teleporter for '" + StartOfRound.Instance.mapScreen.targetedPlayer.playerUsername + "'\n\n",
				playSyncedClip = -1
			};
		}

		private static int GetRadarTargetIndexMinusOne(int index)
		{
			if (index - 1 < 0)
			{
				return StartOfRound.Instance.mapScreen.radarTargets.Count - 1;
			}
			return index - 1;
		}

		private static int SwitchRadarTargetBack()
		{
			// Code copied from ManualCameraRenderer.updateMapTarget with modification to
			ManualCameraRenderer mapScreen = StartOfRound.Instance.mapScreen;
			int index = GetRadarTargetIndexMinusOne(mapScreen.targetTransformIndex);
			PlayerControllerB component;
			for (int i = 0; i < mapScreen.radarTargets.Count; i++)
			{
				if (mapScreen.radarTargets[index] == null)
				{
					index = GetRadarTargetIndexMinusOne(index);
					continue;
				}
				component = mapScreen.radarTargets[index].transform.gameObject.GetComponent<PlayerControllerB>();
				if (component != null && (component.isPlayerControlled || component.isPlayerDead))
				{
					break;
				}
				index = GetRadarTargetIndexMinusOne(index);
			}
			return index;
		}

		private static TerminalNode ToggleHangarDoor(bool openDoor)
		{
			GameObject gameObject = GameObject.Find("Environment/HangarShip/AnimatedShipDoor/HangarDoorButtonPanel/" + (openDoor ? "StartButton" : "StopButton"));
			if (gameObject != null)
			{
				AnimatedObjectTrigger componentInChildren = gameObject.GetComponentInChildren<AnimatedObjectTrigger>();
				if (componentInChildren != null)
				{
					componentInChildren.TriggerAnimationNonPlayer(false, false, false);
					return new TerminalNode
					{
						displayText = (openDoor ? "Opened" : "Closed") + " Hangar Doors\n\n",
						playSyncedClip = -1
					};
				}
			}
			return new TerminalNode
			{
				displayText = "Failed to use Hangar Doors\n\n",
				playSyncedClip = -1
			};
		}

		private static void DisableTurretsAndMines()
		{
			TerminalAccessibleObject[] terminalCodes = UnityEngine.Object.FindObjectsOfType<TerminalAccessibleObject>();
			foreach (TerminalAccessibleObject code in terminalCodes)
			{
				if (!code.isBigDoor)
				{
					code.CallFunctionFromTerminal();
				}
			}
		}

		private static (int, string) FindRadarNumFromName(string input)
		{
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
