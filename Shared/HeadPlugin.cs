using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using MoreHeadUtilities;

using HarmonyLib;
using HarmonyLib;
using MoreHead;

namespace MoreHeadUtilities.Plugin
{
    [BepInPlugin("com.maygik.moreheadutilities", "MoreHeadUtilities", "1.0.0")]
    public class MoreHeadUtilitiesPlugin : BaseUnityPlugin
    {

        public static ConfigEntry<bool> _enableDebugLogging;

        void Awake()
        {
            _enableDebugLogging = Config.Bind("General", "EnableDebugLogging", false, "Enable debug logging for MoreHeadUtilities.");

            if (_enableDebugLogging.Value)
            {
                PartShrinker.Init(Logger);
                Logger?.LogInfo("MoreHeadUtilities initialized and logger passed to PartShrinker.");
                HiddenParts.Init(Logger);
                Logger?.LogInfo("MoreHeadUtilities initialized and logger passed to HiddenParts.");
                MoreHead.Logger.Init(Logger);
            }

            var harmony = new Harmony("com.maygik.moreheadutilities");
            harmony.PatchAll();
            Logger?.LogInfo("Harmony patches applied.");
        }
    }
}
