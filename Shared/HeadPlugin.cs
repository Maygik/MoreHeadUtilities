using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using MoreHeadUtilities;

namespace MoreHeadUtilities.Plugin
{
    [BepInPlugin("com.maygik.moreheadutilities", "MoreHeadUtilities", "1.0.0")]
    public class MoreHeadUtilitiesPlugin : BaseUnityPlugin
    {

        public static ConfigEntry<bool> _enableDebugLogging;

        void Awake()
        {


            PartShrinker.Init(Logger);
            Logger.LogInfo("MoreHeadUtilities initialized and logger passed to PartShrinker.");
            HiddenParts.Init(Logger);
            Logger.LogInfo("MoreHeadUtilities initialized and logger passed to HiddenParts.");

            _enableDebugLogging = Config.Bind("General", "EnableDebugLogging", false, "Enable debug logging for MoreHeadUtilities.");


        }
    }
}
