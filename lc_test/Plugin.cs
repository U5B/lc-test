using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;

namespace lc_test
{

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {

        public const string GUID = "net.usbwire.usb.lc_test";
        public const string NAME = "lc_test";
        public const string VERSION = "1.0.0";

        internal static ManualLogSource mls;
        private void Awake()
        {
            mls = Logger;
            // Plugin startup logic
            mls.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Harmony harmony = new Harmony(GUID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}